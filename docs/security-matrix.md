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
| Ownership on request edit/cancel/detail | IMPLEMENTED | Non-owner customer → 403; missing → 404. |
| Provider eligibility hides ineligible detail | IMPLEMENTED | Ineligible → 404. Same query as `/available`. |
| No customer contact/email on provider request or offer list | IMPLEMENTED | Provider DTOs omit email/contact. `EligibilityTests.Provider_CannotSeeCustomerContactBeforeAcceptance`. |
| Contact visible to booking participants after accept | IMPLEMENTED | Booking detail includes counterpart email; customer `DefaultContact` for participants only. Admin can load request detail with customer email. |
| Offer submit requires eligibility + Open + unique non-withdrawn offer | IMPLEMENTED | 403 if unverified/suspended; 404 if otherwise ineligible; 409 if duplicate active offer or unique-index race. |
| Withdraw only by owning provider, Pending → Withdrawn | IMPLEMENTED | Other providers 403; illegal state 409. |
| Accept only by request owner | IMPLEMENTED | Other customers 403. |
| Single accepted offer / single booking | IMPLEMENTED | State + rowversion + filtered unique index. Second accept 409. |
| Booking start/complete only by provider owner | IMPLEMENTED | Illegal transitions 409. |
| Booking cancel by a participant, Scheduled only, reason required | IMPLEMENTED | Non-participant 403; other statuses 409. |
| Review only by booking customer, Completed, once, rating 1–5 | IMPLEMENTED | 403 / 409 / 400. Rating recompute in the same transaction. |
| Admin-only stats, verification, suspension, users, audit, catalog | IMPLEMENTED | Non-admin 403. Approval endpoint replaced by verification decision. |
| Catalog delete blocked when in use | IMPLEMENTED | Category with services or service with providers/requests → 409. |
| Pagination cap (max 50; audit max 100) | IMPLEMENTED | `PageQuery.Normalize`. Audit uses 25/100. `PaginationTests`. |
| SPA fallback does not swallow `/api/*` 404s | IMPLEMENTED | `MapFallback("/api/{**slug}")` returns Problem Details 404. |
| No auto-migrate / seed in Production | IMPLEMENTED | Guarded by `IsDevelopment()`. |
| Global exception handler does not leak exception text | IMPLEMENTED | Generic 500 title; `ProductionExceptionTests` asserts no stack / exception text. |
| Passwords and connection strings not in source | IMPLEMENTED | User secrets for `ConnectionStrings:Default`, `Seed:AdminPassword`, `Seed:DemoPassword`. |
| Professional document type/size/magic-byte validation | IMPLEMENTED | PDF/JPEG/PNG only, 10 MB, magic bytes. `VerificationDocumentTests`. |
| Documents stored privately, GUID names, authorized download only | IMPLEMENTED | `LocalProviderDocumentStorage` under `App_Data/`; not `wwwroot`. |
| Approve provider requires an approved document | IMPLEMENTED | 409 otherwise; denial audited. |
| Suspension rejects pending offers, keeps bookings | IMPLEMENTED | `SuspensionTests`. |
| Offer while unverified or suspended returns 403 | IMPLEMENTED | Checked before 404 eligibility. Audited as Denied. |
| Append-only audit log, no delete/update API | IMPLEMENTED | `AuditLogTests`. Failure to write audit does not fail the request. |
| Audit details exclude passwords, cookies, tokens, binaries | IMPLEMENTED | Explicit details dictionary only. |
| LastLoginAt set on successful login only | IMPLEMENTED | `AdminUserMonitoringTests`. |
| Concurrent accept under SQL Server | VERIFIED | 18 September 2026. `scripts/concurrency-check.ps1` against `https://localhost:5001` (provider1 + provider4): `Accept statuses: 200, 409`, 409 body contains `Another offer was accepted first.`, exactly one booking, sibling offer `Rejected`. `$env:KHIDMA_SQLSERVER_TESTS=1; dotnet test -c Release` → **119 passed**, 0 failed, 0 skipped (`SqlServerIntegrationTests.ConcurrentAccept_ProducesOneBooking_And409ForLoser` + filtered unique index). |
| HTTPS cookie Secure in production hosting | IMPLEMENTED | `CookieSecurePolicy.Always` for Identity and antiforgery cookies when not Development and not Testing. Readable `XSRF-TOKEN` is Secure in that same case. `UseHsts()` outside Development. App Service HTTPS Only is in `deploy/AZURE_DEPLOY.md` (not applied on this machine). |
| SQL injection via city/status filters | VERIFIED | All list filters go through EF parameterized LINQ (`ServiceRequestService`, `AdminService`). No string-concatenated SQL. |
| XSS in request title/offer message/review comment | VERIFIED | No `dangerouslySetInnerHTML` under `client/src`. React text interpolation only. |
| Admin suspend of a live provider | IMPLEMENTED | `SuspensionTests` (SQLite). Live UI suspend/reactivate was not re-run in the browser in Phase 9. |
| SPA deep link vs `/api` 404 | IMPLEMENTED | `SpaFallbackTests`: `/customer/requests/1` → HTML; `/api/nope` → JSON 404. |
| Generic 500 in Production | IMPLEMENTED | `ProductionExceptionTests` — no exception type, message, or stack in the body. |

## Schema verification (SQL Server)

Query: [`scripts/verify-schema.sql`](../scripts/verify-schema.sql). Expected objects: `UX_Offer_OneAcceptedPerRequest` (`[Status] = 'Accepted'`), `IX_Offers_ServiceRequestId_ProviderId` (`[Status] <> 'Withdrawn'`), unique `IX_Reviews_BookingId`, unique `IX_Bookings_OfferId`, unique `IX_ProviderServices_ProviderProfileId_ServiceId`, `CK_Review_Rating`, `rowversion` on `ServiceRequests` and `Bookings`, `decimal(18,2)` money columns, `nvarchar` status enums.

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

Migrations applied: `20260904230532_InitialCreate`, `20260916111438_AddProviderVerificationAuditAndSuspension`. Host recovery (NVMe sector workaround) is documented in the README Prerequisites and ADR 22 — not a schema bug.

## HTTP walk (§8.4, Week 4 Day 1)

Live script: `scripts/security-matrix.ps1` plus `server/Khidma.Api/Khidma.Api.http`. Direct-URL ineligible provider is **404** (ADR 2), never 200. CSRF missing token is **400** (ADR 17).

| Check | Expected | Automated evidence | Live SQL Server |
| --- | --- | --- | --- |
| Provider B on Provider A's offer | 403/404 | `AuthorizationMatrixTests.ProviderB_CannotWithdrawProviderAOffer` | VERIFIED — `security-matrix.ps1` withdraw 403/404 |
| Provider B on Provider A's booking | 403/404 | `BookingTests` ownership + script | VERIFIED — GET booking 403/404 |
| Provider B on ineligible request (list) | 200 empty | `EligibilityTests.WrongCity_DoesNotSeeRequest` | VERIFIED — `/available` 200, request absent |
| Provider B on ineligible request (URL) | 404 | `EligibilityTests.IneligibleProvider_DetailIsHidden` | VERIFIED — GET detail 404 |
| Unapproved / suspended offer | 403 | `OfferTests` / `SuspensionTests` | VERIFIED — ineligible Provider B POST offer 403 (script); unapproved/suspended remain xUnit |
| No customer contact pre-accept | omitted from JSON | `EligibilityTests.Provider_CannotSeeCustomerContactBeforeAcceptance` | VERIFIED — provider JSON had no contact fields |
| Register `Admin` / `admin` / ` Admin ` | 400 | `AuthEndpointsTests.Register_Admin_IsRejected` | VERIFIED — all three 400 |
| Mutation without `X-XSRF-TOKEN` | 400 | `CsrfMutationTests` | VERIFIED — logout without token 400 |
| Complete Scheduled booking | 409 | `BookingTests.Scheduled_ToCompleted_IsIllegal` | VERIFIED — complete Scheduled 409 |
| Accept on Booked request | 409 | `AcceptOfferTests.SecondAcceptance_IsRejected` | VERIFIED — second accept 409 |
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



