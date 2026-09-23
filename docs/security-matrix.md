# Security matrix

Statuses:

- **IMPLEMENTED** — enforced in code and covered by automated tests or a deterministic code path.
- **TO VERIFY** — implemented but still needs a manual pass against a running SQL Server instance.

| Control | Status | Notes |
| --- | --- | --- |
| Cookie auth, HttpOnly, no JWT in browser storage | IMPLEMENTED | Identity cookie; React uses `credentials: 'include'` only. |
| Anonymous `/api/auth/me` returns JSON 401, not HTML | IMPLEMENTED | Cookie events write Problem Details. `AuthEndpointsTests`. |
| CSRF on unsafe methods | IMPLEMENTED | `AutoValidateAntiforgeryToken`; missing token → **400** (ADR 17). `CsrfMutationTests`. |
| Role gates on controllers | IMPLEMENTED | `[Authorize(Roles=...)]` on customer/provider/admin actions. `AuthorizationMatrixTests`. |
| Booking ownership | IMPLEMENTED | A stranger cannot read a booking (`DirectBookingTests.Stranger_CannotSeePhones` → 403). Missing → 404. |
| Provider must be approved and offer the service | IMPLEMENTED | Unapproved or wrong service → 409. Catalog provider list is approved, not suspended, and optional city. `ServiceProvidersListingTests`. |
| Phone required at registration | IMPLEMENTED | `AccountTests.Register_WithoutPhone_IsRejected`. Public provider JSON has no phone (`ProviderProfileTests.PublicProfile_DoesNotExposeEmail`). |
| Phones visible on the booking to participants | IMPLEMENTED | `BookingDetailDto` returns both phones to the customer and the provider. `DirectBookingTests.Customer_CanBookApprovedProvider_AndBothSeePhones`. |
| One pending booking per customer, provider, and service | IMPLEMENTED | Filtered unique index `IX_Bookings_CustomerId_ProviderId_ServiceId` where `Status = 'Pending'`. Duplicate create → 409. |
| Accept and decline only by the booking's provider | IMPLEMENTED | Another provider → 403 (`AuthorizationMatrixTests.ProviderB_CannotAcceptProviderABooking`). Customer cannot accept → 403. Pending → Scheduled quotes a price, a start, and a duration. Decline requires a reason. Duration is checked in the service after the provider and status checks, so a bad duration from the wrong provider is still 403. |
| Working hours belong to the signed-in provider | IMPLEMENTED | `GET/PUT /api/providers/me/working-hours`. A customer → 403. `ProviderScheduleTests`. |
| Closed weekday cannot be requested | IMPLEMENTED | Create requires at least one working hour on that weekday, and the day must be today or later. `ProviderScheduleTests`. |
| Accept and reschedule reject overlapping visits | IMPLEMENTED | Scheduled and InProgress intervals for the same provider. Overlap → 409 with the other visit's time range. A start outside published hours is allowed. `ProviderScheduleTests`. |
| Reschedule only by the provider, and only while Scheduled or InProgress | IMPLEMENTED | `POST /api/bookings/{id}/schedule`. Sets `RescheduledAt` and an optional note. Audited as `Booking.Rescheduled`. |
| Public availability hides who is busy | IMPLEMENTED | `GET /api/providers/{id}/availability` is anonymous, returns working hours and busy intervals only, 404 when the provider cannot receive work, and rejects a range longer than 62 days. |
| Concurrent accept of one booking | IMPLEMENTED | State check plus `rowversion`. Loser → 409. `SqlServerIntegrationTests.ConcurrentAccept_OneSucceeds_AndLoserConflicts` when SQL Server tests are enabled. |
| Booking start/complete only by provider owner | IMPLEMENTED | Illegal transitions 409. |
| Customer cancels Pending or Scheduled; provider cancels Scheduled | IMPLEMENTED | Provider cancel of a pending booking → 409 (decline instead). Reason required. |
| Review only by booking customer, Completed, once, rating 1–5 | IMPLEMENTED | 403 / 409 / 400. Rating recompute in the same transaction. |
| Admin-only stats, verification, suspension, users, audit, catalog | IMPLEMENTED | Non-admin 403. Approval endpoint replaced by verification decision. |
| Catalog delete blocked when in use | IMPLEMENTED | Category with services, or service with providers or bookings → 409. |
| Pagination cap (max 50; audit max 100) | IMPLEMENTED | `PageQuery.Normalize`. Audit uses 25/100. `PaginationTests`. |
| SPA fallback does not swallow `/api/*` 404s | IMPLEMENTED | `MapFallback("/api/{**slug}")` returns Problem Details 404. |
| No auto-migrate / seed in Production | IMPLEMENTED | Guarded by `IsDevelopment()`. |
| Global exception handler does not leak exception text | IMPLEMENTED | Generic 500 title; `ProductionExceptionTests` asserts no stack / exception text. |
| Passwords and connection strings not in source | IMPLEMENTED | User secrets for `ConnectionStrings:Default`, `Seed:AdminPassword`, `Seed:DemoPassword`. |
| Professional document type/size/magic-byte validation | IMPLEMENTED | PDF/JPEG/PNG only, 10 MB, magic bytes. `VerificationDocumentTests`. |
| Documents stored privately, GUID names, authorized download only | IMPLEMENTED | `LocalProviderDocumentStorage` under `App_Data/`; not `wwwroot`. |
| Approve provider requires an approved document | IMPLEMENTED | 409 otherwise; denial audited. |
| Suspension declines pending bookings and keeps scheduled work | IMPLEMENTED | `SuspensionTests`. The provider is hidden from the catalog provider list and cannot be booked. |
| Append-only audit log, no delete/update API | IMPLEMENTED | `AuditLogTests`. Failure to write audit does not fail the request. |
| Audit details exclude passwords, cookies, tokens, binaries | IMPLEMENTED | Explicit details dictionary only. |
| LastLoginAt set on successful login only | IMPLEMENTED | `AdminUserMonitoringTests`. |
| Concurrent accept under SQL Server | IMPLEMENTED | `SqlServerIntegrationTests.ConcurrentAccept_OneSucceeds_AndLoserConflicts` expects one 200 and one 409, and the booking stays Scheduled. Enable with `KHIDMA_SQLSERVER_TESTS=1`. The 18 September 2026 offer-race script result is historical and no longer matches this model. |
| HTTPS cookie Secure in production hosting | IMPLEMENTED | `CookieSecurePolicy.Always` for Identity and antiforgery cookies when not Development and not Testing. Readable `XSRF-TOKEN` is Secure in that same case. `UseHsts()` outside Development. App Service HTTPS Only is in `deploy/AZURE_DEPLOY.md` (not applied on this machine). |
| SQL injection via city/status filters | VERIFIED | Catalog city and booking status filters go through EF parameterized LINQ (`CatalogService`, `BookingService`, `AdminService`). No string-concatenated SQL. |
| XSS in booking notes, provider message, and review comment | VERIFIED | No `dangerouslySetInnerHTML` under `client/src`. React text interpolation only. |
| Admin suspend of a live provider | IMPLEMENTED | `SuspensionTests` (SQLite). Live UI suspend/reactivate was not re-run in the browser in Phase 9. |
| SPA deep link vs `/api` 404 | IMPLEMENTED | `SpaFallbackTests`: `/account/bookings/1` → HTML; `/api/nope` → JSON 404. |
| Generic 500 in Production | IMPLEMENTED | `ProductionExceptionTests` — no exception type, message, or stack in the body. |

## Schema verification (SQL Server)

Query: [`scripts/verify-schema.sql`](../scripts/verify-schema.sql). After `DirectBookingRedesign`, expected objects include filtered unique `IX_Bookings_CustomerId_ProviderId_ServiceId` (`[Status] = 'Pending'`), unique `IX_Reviews_BookingId`, unique `IX_ProviderServices_ProviderProfileId_ServiceId`, `CK_Review_Rating`, `rowversion` on `Bookings`, `Bookings.QuotedPrice` `decimal(18,2)`, and `Bookings.Status` `nvarchar(20)`. `Offers` and `ServiceRequests` are dropped. The dump below is the 18 September 2026 schema, before that migration.

**Output (18 September 2026)** after LocalDB started (ADR 22 / README NVMe note). Command: `sqlcmd -S "(localdb)\MSSQLLocalDB" -d Khidma -C -W -i scripts/verify-schema.sql`

Every §5.3 constraint is present. No new migration was required.

```text
=== Filtered and unique indexes ===
index_name table_name is_unique filter_definition column_name
---------- ---------- --------- ----------------- -----------
IX_Bookings_OfferId Bookings 1 NULL OfferId
IX_Offers_ServiceRequestId_ProviderId Offers 1 ([Status]<>'Withdrawn') ServiceRequestId
IX_Offers_ServiceRequestId_ProviderId Offers 1 ([Status]<>'Withdrawn') ProviderId
IX_ProviderServices_ProviderProfileId_ServiceId ProviderServices 1 NULL ProviderProfileId
IX_ProviderServices_ProviderProfileId_ServiceId ProviderServices 1 NULL ServiceId
IX_Reviews_BookingId Reviews 1 NULL BookingId
RoleNameIndex AspNetRoles 1 ([NormalizedName] IS NOT NULL) NormalizedName
UserNameIndex AspNetUsers 1 ([NormalizedUserName] IS NOT NULL) NormalizedUserName
UX_Offer_OneAcceptedPerRequest Offers 1 ([Status]='Accepted') ServiceRequestId
=== Check constraints ===
name table_name definition
---- ---------- ----------
CK_ProviderVerificationDocuments_FileSize ProviderVerificationDocuments ([FileSizeBytes]>(0))
CK_Review_Rating Reviews ([Rating]>=(1) AND [Rating]<=(5))
=== Rowversion columns ===
table_name column_name type_name
---------- ----------- ---------
Bookings RowVersion timestamp
ServiceRequests RowVersion timestamp
=== Money decimals ===
table_name column_name type_name precision scale
---------- ----------- --------- --------- -----
Bookings FinalPrice decimal 18 2
Offers Price decimal 18 2
ServiceRequests BudgetMax decimal 18 2
ServiceRequests BudgetMin decimal 18 2
=== String enum columns ===
table_name column_name type_name max_length
---------- ----------- --------- ----------
Bookings Status nvarchar 40
Offers Status nvarchar 40
ProviderProfiles VerificationStatus nvarchar 64
ServiceRequests Status nvarchar 40
```

Migrations applied on that date: `20260904230532_InitialCreate`, `20260916111438_AddProviderVerificationAuditAndSuspension`. A later migration, `AddProviderScheduleAndBookingSlots`, adds `ProviderWorkingHours` (unique on provider, weekday, and hour) and booking slot columns: `RequestedDate` as a date, `ScheduledStart`, `DurationHours` (1–12), `RescheduledAt`, and `RescheduleNote`. Apply it with `deploy/migrate.sql`. Host recovery (NVMe sector workaround) is documented in the README Prerequisites and ADR 22 — not a schema bug.

## HTTP walk (§8.4, Week 4 Day 1)

Live script: `scripts/security-matrix.ps1` plus `server/Khidma.Api/Khidma.Api.http`. Direct-URL ineligible provider is **404** (ADR 2), never 200. CSRF missing token is **400** (ADR 17).

| Check | Expected | Automated evidence | Live SQL Server |
| --- | --- | --- | --- |
| Provider B accepts Provider A's booking | 403 | `AuthorizationMatrixTests.ProviderB_CannotAcceptProviderABooking` | Automated |
| Stranger reads a booking | 403 | `DirectBookingTests.Stranger_CannotSeePhones` | Automated |
| Unapproved provider cannot be booked | 409 | `DirectBookingTests.UnapprovedProvider_CannotBeBooked` | Automated |
| Suspended provider hidden from the service list | omitted | `SuspensionTests` | Automated |
| Public provider profile has no phone | omitted | `ProviderProfileTests.PublicProfile_DoesNotExposeEmail` | Automated |
| Register `Admin` / `admin` / ` Admin ` | 400 | `AuthEndpointsTests.Register_Admin_IsRejected` | VERIFIED — all three 400 |
| Mutation without `X-XSRF-TOKEN` | 400 | `CsrfMutationTests` | VERIFIED — logout without token 400 |
| Complete Scheduled booking | 409 | `BookingTests.Scheduled_ToCompleted_IsIllegal` | VERIFIED — complete Scheduled 409 |
| Second accept of a booking | 409 | `SqlServerIntegrationTests.ConcurrentAccept_OneSucceeds_AndLoserConflicts` | Automated when SQL Server tests are enabled |
| Second review | 409 | `ReviewTests.SecondReview_IsRejected` | VERIFIED — second review 409 |
| Path traversal filename | 400 | `VerificationDocumentTests.PathTraversalFileName_IsRejected` | VERIFIED — upload `..\..\secret.pdf` 400 |
| Wrong magic bytes | 400 | `VerificationDocumentTests.FakeMimeSignature_IsRejected` | Automated only (not in live script) |
| Oversize (>10 MB) | 400 | `VerificationDocumentTests.FileOver10Mb_IsRejected` | Automated only (not in live script) |
| Other provider downloads document | 403 | `VerificationDocumentTests.AnotherProvider_CannotReadOrDeleteDocument` | Automated only (not in live script) |

Live script result 18 September 2026: `scripts/security-matrix.ps1` **14 passed, 0 failed** against `https://localhost:5001`. `scripts/smoke-test.ps1` **8 passed, 0 failed**.

XSS: no `dangerouslySetInnerHTML` in `client/src`. Titles, messages, and review comments are React text nodes.

## Git history secret scan

Command: `git log -p` filtered for `password|connectionstring|secret` (18 September 2026). Unique added/removed lines were classified. No live production password or Azure connection string appeared.

| Hit class | Verdict |
| --- | --- |
| `appsettings.json` `ConnectionStrings` | Current file has **no** connection string. Historical diffs showed the key / LocalDB-style `Trusted_Connection` design-time fallback in `AppDbContextFactory`, not a SQL login password. |
| `["Seed:AdminPassword"] = "Test_Admin_123!"` / `Test_Demo_123!` | Test-only values in `KhidmaApiFactory`. Not production secrets. |
| `password = "ValidPass1!"` | xUnit fixtures. |
| Identity `PasswordHash` columns in migrations | Schema only. |
| UI `type="password"`, `autoComplete`, show/hide labels | Expected. |
| Scripts reading `KHIDMA_*_PASSWORD` | Env / `Read-Host -AsSecureString`; they do not print the value. |
| CSRF `XSRF-TOKEN` | Expected anti-forgery wiring. |

If a reviewer finds a real credential in history that this pass missed, stop and rotate; do not rewrite history without an explicit decision.

## Admin trust layer (folded from the old security review report)

Verification (`PendingReview` / `Approved` / `Rejected`) is not the same as suspension (`IsSuspended` + reason). Admin cannot approve a provider without an approved document (409). Rejecting a provider or document requires a stored reason. Document downloads are owner-or-admin `File()` results. Audit has no FK and no update/delete API.

Residual (unchanged): uploads are not malware-scanned; audit has no retention job. LocalDB on this NVMe host was recovered with the sector workaround (README Prerequisites, ADR 22).



