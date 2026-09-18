# Engineering Decisions

These ADRs record the choices that shape Khidma’s Weeks 2–3 marketplace. Week 1 cookie-auth decisions remain in force.

## ADR 1 — Cookie authentication, not JWT

**Decision.** ASP.NET Core Identity issues an HttpOnly authentication cookie. The React client never stores access tokens, refresh tokens, or user payloads in `localStorage` or `sessionStorage`. Session restore is `GET /api/auth/me`. Anonymous calls return JSON 401 (Problem Details), not an HTML login redirect.

**Why.** Same-origin hosting (`wwwroot` in production-style runs, Vite `/api` proxy in development) makes a cookie the simplest CSRF-aware session. JWT would add token storage, refresh, and XSS surface without helping this product.

**CSRF.** `GET /api/antiforgery/token` sets a readable `XSRF-TOKEN` cookie. Unsafe methods send `X-XSRF-TOKEN`. Controllers use `AutoValidateAntiforgeryToken`. A missing or invalid token returns **400 Bad Request** (framework default). See ADR 17.

## ADR 2 — Exact-city eligibility

**Decision.** A provider sees an open request only when all of these are true, in one reusable query (`EligibleOpenRequestsForProvider`):

1. Request status is `Open`
2. The provider is professionally verified (`VerificationStatus == Approved`) and not suspended
3. The request’s service is in the provider’s `ProviderServices`
4. `request.City.ToLower() == provider.City.ToLower()`

Ineligible providers receive 404 on request detail, not 403, so they cannot enumerate hidden jobs.

**Why.** The product is a local marketplace. Partial city matching (`Contains`, radius, aliases) would leak work across cities and is out of scope. Case-insensitive equality is enough for seeded Palestinian city names.

## ADR 3 — Booking stores denormalized participant ids

**Decision.** `Booking` stores `CustomerId`, `ProviderId` (Identity user ids), `ServiceRequestId`, and `OfferId`. `Offer.ProviderId` is also the user id.

**Why.** Booking queries (mine, start, complete, cancel, review ownership) filter by participant without joining through Offer → ServiceRequest. The denormalized ids are written once inside the accept transaction from the loaded offer and request.

## ADR 4 — Stored rating aggregates

**Decision.** `ProviderProfile.AverageRating` and `ReviewCount` are stored columns. Creating a review recomputes both in the same transaction: `ReviewCount = count`, `AverageRating = Round(avg, 2)` over every review for that provider.

**Why.** Public profiles and offer cards show rating on every list. Recalculating from `Reviews` on each read is correct but heavier. The write path is rare (one review per completed booking) and already transactional.

## ADR 5 — Three-layer accept-offer defense

**Decision.** Accepting an offer runs inside an execution strategy + transaction and uses three defenses:

1. **State.** Offer must be `Pending` and the request `Open`; otherwise 409. Only the owning customer may accept (else 403).
2. **Rowversion.** `ServiceRequest.RowVersion` is a concurrency token. A racing second accept fails `SaveChanges` with `DbUpdateConcurrencyException` → 409.
3. **Filtered unique index.** `UX_Offer_OneAcceptedPerRequest` (`ServiceRequestId` unique where `Status = 'Accepted'`). A unique `DbUpdateException` also maps to 409.

Sibling `Pending` offers become `Rejected`. The request becomes `Booked`. One `Booking` is inserted (`ScheduledDate = EstimatedDate`, `FinalPrice = Price`, status `Scheduled`).

**Why.** HTTP-level “check then write” is not enough under concurrent accepts. Database constraints are the last word.

## ADR 6 — SQLite is test-only and has limits

**Decision.** Production uses SQL Server. Automated tests use SQLite in-memory (`KhidmaApiFactory`). `AppDbContext.ApplySqliteTestCompatibility` maps SQL Server `rowversion` to a nullable BLOB with `ValueGenerated.Never` so inserts succeed. Filtered-index filters rewrite `[Status]` to `"Status"` for SQLite.

**Limits.** SQLite cannot `ORDER BY DateTimeOffset`. List endpoints order by identity `Id` (monotonic) instead of `CreatedAt`. SQLite filtered unique indexes are not equivalent to SQL Server’s; accept races are still asserted via state + 409 mapping.

**Why.** Tests stay fast and local without SQL Server. The shim never runs against SQL Server.

## ADR 7 — Pagination cap

**Decision.** `PageQuery` normalizes `page ≥ 1`, default `pageSize = 20`, max `pageSize = 50`. Responses are `PagedResult<T>` (`items`, `page`, `pageSize`, `totalCount`, `totalPages`).

**Why.** Unbounded lists are an easy denial-of-service and a poor UI. 50 is enough for admin/provider browsing.

## ADR 8 — No auto-migrate in Production

**Decision.** `Database.MigrateAsync()` and the idempotent seeder run only when `Environment.IsDevelopment()`. Production must apply `dotnet ef migrations script --idempotent` (or an equivalent deploy step). There is still a single InitialCreate migration; Weeks 2–3 added no schema migrations.

**Why.** Auto-migrate in production races with multiple instances and hides DBA review.

## ADR 9 — Providers pending review by default

**Decision.** New provider registration sets `ProviderProfile.VerificationStatus = PendingReview` (replacing the old `IsApproved` flag). Unverified providers cannot receive new work. Admin approval requires at least one **Approved** professional document; otherwise the decision API returns 409. Rejection requires a reason.

The seeder keeps `provider1` and `provider3` as `Approved` and `provider2` as `PendingReview`.

**Why.** Professional proof is the trust gate. A boolean approval toggle let an admin mark a provider live with no documents on file.

## ADR 10 — Dashboard summary endpoints


**Decision.** `GET /api/dashboard/customer` and `GET /api/dashboard/provider` return counters plus a short recent list. They are not a substitute for the paged list endpoints.

**Why.** Role home pages need real numbers (open requests, offers awaiting decision, eligible requests, rating). Composing four list calls on every dashboard load is slower and harder to keep consistent.

## ADR 11 — Two provider identifiers

**Decision.**

| Field | Meaning |
| --- | --- |
| `Offer.ProviderId` / `Booking.ProviderId` | ASP.NET Identity user id (`string`) |
| `ProviderProfile.Id` | Public/admin integer id |

DTOs that link to a public profile include both `providerId` and `providerProfileId`. Admin approval and `GET /api/providers/{id}` use the profile id.

**Why.** Offers and bookings are ownership records tied to the signed-in user. The public page is a catalog resource keyed by a stable integer.

## ADR 12 — Thin controllers, DTO-only responses

**Decision.** Controllers parse the user id and bind the request, call a service, and map `ServiceResult<T>` through `ApiControllerBase.FromResult` to 200, `ValidationProblemDetails` (400), or `ProblemDetails` (401/403/404/409). Entities are never serialized.

**Why.** Authorization attributes stay at the HTTP edge; eligibility, ownership, and state machines live in one place and are unit-tested through WebApplicationFactory.

## ADR 13 — List sort uses Id on SQLite-safe queries

**Decision.** Recent-item and mine lists order by descending `Id`, not `CreatedAt`.

**Why.** Identity columns are monotonic with insert time. This keeps the same LINQ provider path on SQL Server and SQLite (see ADR 6).

## ADR 14 — Eligibility is approved and not suspended

**Decision.** `ServiceRequestService.EligibleOpenRequestsForProvider` is the only eligibility source. A provider can receive work when:

1. `VerificationStatus == Approved`
2. `IsSuspended == false`
3. matching service
4. exact city (case-insensitive)
5. request is `Open`

`GET /api/service-requests/available` returns **200 with an empty page** for unverified or suspended providers (same convention as the old unapproved list). `POST /api/service-requests/{id}/offers` returns **403** when the provider is not approved or is suspended, checked **before** the existing 404 ineligible-request path. Suspension rejects `Pending` offers and never mutates bookings.

**Why.** Hiding the marketplace (empty 200) avoids leaking demand to ineligible accounts. 403 on offer is an explicit policy denial and is audited as `Denied`. Bookings already in flight must still be completable.

## ADR 15 — Persistent append-only audit log

**Decision.** `AuditLog` has no foreign keys and no update/delete endpoints. `IAuditService.RecordAsync` captures actor claims, IP, user agent, and `TraceIdentifier`, serializes only an explicit details dictionary, and swallows persistence failures after logging them. Call sites run after the business commit.

**Why.** Trust and moderation actions need a durable trail. Audit must not take down the request that succeeded. No FKs means deleting a user cannot cascade-erase history.

## ADR 16 — Private professional documents

**Decision.** Providers upload PDF/JPEG/PNG files (1 byte–10 MB) with matching extension, content type, and magic bytes. Storage is `IProviderDocumentStorage` rooted at `ProviderDocuments:RootPath` (default `{ContentRoot}/App_Data/provider-documents`). Stored names are GUIDs. Downloads go through authorized `File()` results, never static files.

**Why.** These files are identity evidence, not a public portfolio. `wwwroot` would make them anonymously fetchable.

## ADR 17 — CSRF failure is 400

**Decision.** Keep ASP.NET Core's default for `AutoValidateAntiforgeryToken`: a missing or invalid `X-XSRF-TOKEN` is **400 Bad Request**, not 403. Plan v2 § Week 4 Day 1 said 403; the framework default is 400 (`AntiforgeryValidationFailedResult`). Tests (`CsrfMutationTests`, `AuthEndpointsTests.PostWithoutAntiforgery_IsRejected`), `scripts/smoke-test.ps1`, and `scripts/security-matrix.ps1` all assert 400.

**Why.** Overriding the filter to 403 would be a custom convention with no security gain. Reviewers and the client already treat any non-2xx as failure. One number everywhere beats a 400/403 split.

**Rejected alternative.** Map antiforgery failures to 403 Forbidden so the status matches "you are not allowed." That confuses CSRF (the request is malformed) with authorization (the user is the wrong principal).

## ADR 18 — Dashboard and available-feed query counts

**Decision.** Enable `Microsoft.EntityFrameworkCore.Database.Command` = Information and `EnableSensitiveDataLogging` in Development only. Dashboards and `GET /api/service-requests/available` already use `Select` projections (no per-row round trips).

| Endpoint | Round trips (LINQ, after this pass) |
| --- | --- |
| `GET /api/dashboard/customer` | 6 (`Count` × 4 + recent requests + active bookings) |
| `GET /api/dashboard/provider` | 7 (profile + eligible count + pending offers + active bookings + 3 projected lists) |
| `GET /api/admin/stats` | One `CountAsync` per counter (independent aggregates, not N+1) |
| `GET /api/service-requests/available` | 2 (provider profile + one projected page) |

Live `Executed DbCommand` capture against SQL Server is blocked until LocalDB starts. There was no N+1 to fix; counts above are the after state and match the before state in source.

**Pagination.** `PageQuery` caps `pageSize` at 50 (audit logs 100). Catalog lists and `GET /service-requests/{id}/offers` are unpaged because they are bounded by the catalog size and the unique-offer-per-provider rule.

