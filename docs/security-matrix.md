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
| Concurrent accept under SQL Server | FAILED | LocalDB 17.0.4025.3 crashes on start: `256 misaligned log IOs` on `master.mdf` (NVMe 32K physical sectors). Recreate (`sqllocaldb delete`/`create -s`) did not help. Live scripts and `KHIDMA_SQLSERVER_TESTS=1` are blocked until the host SQL instance starts. Code path unified to `Another offer was accepted first.` Opt-in tests: `SqlServerIntegrationTests`. |
| HTTPS cookie Secure in production hosting | IMPLEMENTED | `CookieSecurePolicy.Always` for Identity and antiforgery cookies when not Development and not Testing. Readable `XSRF-TOKEN` is Secure in that same case. `UseHsts()` outside Development. App Service HTTPS Only is in `deploy/AZURE_DEPLOY.md` (not applied on this machine). |
| SQL injection via city/status filters | VERIFIED | All list filters go through EF parameterized LINQ (`ServiceRequestService`, `AdminService`). No string-concatenated SQL. |
| XSS in request title/offer message/review comment | VERIFIED | No `dangerouslySetInnerHTML` under `client/src`. React text interpolation only. |
| Admin suspend of a live provider | IMPLEMENTED | `SuspensionTests` (SQLite). Live UI confirm still blocked on SQL Server. |
| SPA deep link vs `/api` 404 | IMPLEMENTED | `SpaFallbackTests`: `/customer/requests/1` → HTML; `/api/nope` → JSON 404. |
| Generic 500 in Production | IMPLEMENTED | `ProductionExceptionTests` — no exception type, message, or stack in the body. |

## Schema verification (SQL Server)

Query: [`scripts/verify-schema.sql`](../scripts/verify-schema.sql). Expected objects: `UX_Offer_OneAcceptedPerRequest` (`[Status] = 'Accepted'`), `IX_Offers_ServiceRequestId_ProviderId` (`[Status] <> 'Withdrawn'`), unique `IX_Reviews_BookingId`, unique `IX_Bookings_OfferId`, unique `IX_ProviderServices_ProviderProfileId_ServiceId`, `CK_Review_Rating`, `rowversion` on `ServiceRequests` and `Bookings`, `decimal(18,2)` money columns, `nvarchar` status enums.

**Output (18 September 2026):** not executed. `sqllocaldb start MSSQLLocalDB` failed after stop/delete/create. `error.log`:

```text
There have been 256 misaligned log IOs which required falling back to synchronous IO.
The current IO is on file ...\MSSQLLocalDB\master.mdf.
Hit Fatal Error: Server is terminating
```

This is the SQL Server 2022+ NVMe 32 KB physical-sector issue, not a Khidma schema bug. Installing SQL Server 2022 Developer/Express on the same disk will hit the same crash unless the sector workaround is applied first.

### Unblock SQL Server on this machine

1. In an elevated Command Prompt:

```bat
REG ADD "HKLM\SYSTEM\CurrentControlSet\Services\stornvme\Parameters\Device" /v ForcedPhysicalSectorSizeInBytes /t REG_MULTI_SZ /d "* 4095" /f
```

2. Reboot.
3. `sqllocaldb start MSSQLLocalDB`
4. Confirm: `sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "SELECT @@VERSION" -C`
5. Then:

```bat
dotnet user-secrets set "ConnectionStrings:Default" "Server=(localdb)\mssqllocaldb;Database=Khidma;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True" --project server/Khidma.Api
dotnet ef database update --project server/Khidma.Api
sqlcmd -S "(localdb)\MSSQLLocalDB" -d Khidma -C -i scripts/verify-schema.sql
set KHIDMA_SQLSERVER_TESTS=1
dotnet test -c Release --filter FullyQualifiedName~SqlServerIntegrationTests
```

If you would rather install a full instance after the registry fix: SQL Server 2022 Developer, mixed mode or Windows auth, enable TCP, then put that connection string in user-secrets instead of LocalDB.

## HTTP walk (§8.4, Week 4 Day 1)

Live script: `scripts/security-matrix.ps1` plus `server/Khidma.Api/Khidma.Api.http`. Direct-URL ineligible provider is **404** (ADR 2), never 200. CSRF missing token is **400** (ADR 17).

| Check | Expected | Automated evidence | Live SQL Server |
| --- | --- | --- | --- |
| Provider B on Provider A's offer | 403/404 | `AuthorizationMatrixTests.ProviderB_CannotWithdrawProviderAOffer` | Blocked (LocalDB down) |
| Provider B on Provider A's booking | 403/404 | `BookingTests` ownership + script | Blocked |
| Provider B on ineligible request (list) | 200 empty | `EligibilityTests.WrongCity_DoesNotSeeRequest` | Blocked |
| Provider B on ineligible request (URL) | 404 | `EligibilityTests.IneligibleProvider_DetailIsHidden` | Blocked |
| Unapproved / suspended offer | 403 | `OfferTests` / `SuspensionTests` | Blocked |
| No customer contact pre-accept | omitted from JSON | `EligibilityTests.Provider_CannotSeeCustomerContactBeforeAcceptance` | Blocked |
| Register `Admin` / `admin` / ` Admin ` | 400 | `AuthEndpointsTests.Register_Admin_IsRejected` | Blocked |
| Mutation without `X-XSRF-TOKEN` | 400 | `CsrfMutationTests` | Blocked |
| Complete Scheduled booking | 409 | `BookingTests.Scheduled_ToCompleted_IsIllegal` | Blocked |
| Accept on Booked request | 409 | `AcceptOfferTests.SecondAcceptance_IsRejected` | Blocked |
| Second review | 409 | `ReviewTests.SecondReview_IsRejected` | Blocked |
| Path traversal filename | 400 | `VerificationDocumentTests.PathTraversalFileName_IsRejected` | Blocked |
| Wrong magic bytes | 400 | `VerificationDocumentTests.FakeMimeSignature_IsRejected` | Blocked |
| Oversize (>10 MB) | 400 | `VerificationDocumentTests.FileOver10Mb_IsRejected` | Blocked |
| Other provider downloads document | 403 | `VerificationDocumentTests.AnotherProvider_CannotReadOrDeleteDocument` | Blocked |

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

Residual (unchanged): uploads are not malware-scanned; audit has no retention job; LocalDB on this NVMe host still needs the sector workaround before live SQL proofs.



