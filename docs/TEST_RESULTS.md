# Test results — Admin / Trust / Audit pass

Recorded 16 September 2026 on `feature/full-project-development`. Automated commands were run locally. Nothing was pushed.

## Baseline (before this pass)

Recorded at HEAD `979ab3d` before the domain work:

| Suite | Result |
| --- | --- |
| Backend `dotnet test -c Release` | 75 passing test cases (~70 Facts/Theories) |
| Frontend Vitest | 11 passing tests |

## Automated pass (after this pass)

| Command | Result |
| --- | --- |
| `dotnet restore` / `dotnet build Khidma.sln -c Release` | Succeeded |
| `dotnet test -c Release` | **109 passed**, 0 failed, 0 skipped (8 s) |
| `npm run lint` (client) | Passed after fixing a useless regex escape in `api/client.ts` |
| `npx tsc -b` (client) | Passed |
| `npm run test` (client) | **25 passed**, 0 failed, 13 files (5.4 s) |
| `npm run build` (client) | Passed (Vite production build to `server/Khidma.Api/wwwroot`) |
| `dotnet ef migrations list` | Listed `20260904230532_InitialCreate` and `20260916111438_AddProviderVerificationAuditAndSuspension`. Applied status unknown — LocalDB did not start (error 50). |
| `dotnet ef migrations has-pending-model-changes` | **No pending model changes** |
| `dotnet ef database update` | **Not applied.** LocalDB failed to start (`SQL Network Interfaces, error: 50 - Local Database Runtime error occurred`). The same connection failure was observed on `migrations list`. Auto-review also blocked a later `database update` attempt because it would mutate a shared database. |

`npm ci` was not re-run; `client/node_modules` was already installed and lint/tsc/test/build succeeded against that tree.

Live scripts `scripts/smoke-test.ps1` and `scripts/concurrency-check.ps1` were **not** executed against a running API (no healthy SQL Server instance). They remain manual checks.

## Backend coverage added

New / extended xUnit classes:

- `VerificationDocumentTests` — PDF/JPEG/PNG accepted; exe, mismatched extension, fake magic bytes, and oversize rejected; access matrix; delete rules; reject note required
- `ProviderVerificationDecisionTests` — 0/pending/rejected documents → 409; approved document → 200; rejection reason; re-upload returns `PendingReview`; eligibility after approval
- `SuspensionTests` — pending offers rejected; accepted offers/bookings untouched; complete `InProgress` while suspended; reactivation
- `AuditLogTests` — event coverage; no password/cookie/XSRF/binary in `DetailsJson`; authz; no DELETE route
- `AdminUserMonitoringTests` — list/filter/search/paging; `LastLoginAt` on successful login only
- `FullMarketplaceWorkflowTests` — end-to-end marketplace including verification
- `CsrfMutationTests` — missing `X-XSRF-TOKEN` → 400; valid token → 200

Existing `AdminTests` / `EligibilityTests` / `OfferTests` / `ProviderProfileTests` were adapted to `VerificationStatus` and the 403-on-offer rule without deleting cases.

## Frontend coverage added

Vitest cases now include:

- `AdminUsersPage` loads users
- `AdminVerificationsPage` loading / empty / error / success
- `AdminVerificationDetailPage` approve disabled until an approved document exists; reject requires a reason
- `AdminProvidersPage` suspend requires a reason; reactivate confirms
- `AdminAuditPage` applies filters and opens a detail drawer
- `ProviderDashboard` suspended banner (`role="alert"`)
- `ProviderProfilePage` document statuses and admin notes

Existing login, role-gate, request, offer, review, and booking tests still pass.

## Secret scan

`rg -i "password|connectionstring|secret|token"` over source (excluding `node_modules`, `bin`, `obj`).

| Hit class | Verdict |
| --- | --- |
| User-secrets documentation (`ConnectionStrings:Default`, `Seed:*Password`) | Placeholders only. `appsettings.json` has no connection strings. |
| Design-time LocalDB string in `AppDbContextFactory` | Documented fallback for `dotnet ef` only. Not a production secret. |
| Test password `ValidPass1!` in xUnit helpers | Test-only fixture, same as the previous marketplace pass. |
| CSRF `XSRF-TOKEN` / `X-XSRF-TOKEN` | Expected anti-forgery wiring. |
| UI password fields | Expected. |
| Smoke/concurrency scripts reading `KHIDMA_*_PASSWORD` | Values come from env or `Read-Host -AsSecureString`; scripts never `Write-Host` the secret. |

No live passwords, API keys, or production connection strings were found in git-tracked files.
