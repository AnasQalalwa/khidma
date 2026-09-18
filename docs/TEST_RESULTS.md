# Test results — Week 4 closeout

Recorded 18 September 2026 on `feature/full-project-development` at HEAD `2db952d`.

## Phase 0 baseline (before closeout code changes)

Commands were run as-is. **No failures.** Nothing was changed to make the suite pass.

| Command | Result |
| --- | --- |
| `dotnet build -c Release -warnaserror` | Succeeded. 0 Warning(s), 0 Error(s). 34 s. |
| `dotnet test -c Release` | **109 passed**, 0 failed, 0 skipped (9 s). 102 `[Fact]` + 2 `[Theory]` (7 InlineData cases) = 109. |
| `npm run lint` (client) | Passed. |
| `npx tsc -b` (client) | Passed (also as part of `npm run build`). |
| `npm run test` (client) | **25 passed**, 0 failed, 13 files (27 s). |
| `npm run build` (client) | Passed. Vite production build to `server/Khidma.Api/wwwroot`. |

### Not run in Phase 0

| Check | Status |
| --- | --- |
| `dotnet ef database update` / schema inspection | Deferred to Phase 1 (needs a healthy SQL Server). Prior record: LocalDB error 50. |
| `scripts/smoke-test.ps1` | Deferred to Phase 1. |
| `scripts/concurrency-check.ps1` | Deferred to Phase 1. |
| SQL Server opt-in tests | Do not exist yet; Phase 1. |

### Backend inventory (109 cases)

19 test classes under `server/Khidma.Api.Tests/`. SQLite in-memory via `KhidmaApiFactory`. Live SQL Server is **not** exercised here (see ADR 6).

### Frontend inventory (25 cases)

13 Vitest files. No Playwright.

### Failures before touching code

None. The gate is green. Subsequent phases must keep it green and must not delete or weaken these tests.

## Phase 1 — Real SQL Server (18 September 2026)

| Check | Result |
| --- | --- |
| Repair LocalDB (`stop`/`delete`/`create -s`) | Instance recreated. Start still fails. |
| Root cause | `error.log`: `256 misaligned log IOs` on `master.mdf` (SQL Server 2025 LocalDB 17.0.4025.3 + NVMe 32K sectors). |
| `dotnet ef database update` | Not applied. |
| `scripts/verify-schema.sql` | Not executed. |
| `scripts/smoke-test.ps1` / `concurrency-check.ps1` | Not executed (no API against SQL Server). |
| `KHIDMA_SQLSERVER_TESTS=1` | Cannot run until LocalDB starts. Tests are skipped by default. |

Code shipped so the live proof is ready the moment SQL Server starts:

- Accept-race 409 title unified to `Another offer was accepted first.`
- SQLite-only `RowVersion = [0]` stamps so SQL Server can generate `rowversion`
- `SqlServerIntegrationTests` (concurrent accept + filtered unique index)
- `scripts/concurrency-check.ps1` asserts the 409 body and optional sibling `Rejected`

Unblock steps are in `docs/security-matrix.md`.

## Phase 2 — Security matrix (18 September 2026)

| Command | Result |
| --- | --- |
| `dotnet test -c Release` | **113 passed**, 0 failed, 2 skipped (SQL Server opt-in). |
| Live `scripts/security-matrix.ps1` | Not executed (no SQL Server). Cases covered by xUnit; script is ready. |

Added: admin registration variants (`Admin`/`admin`/` Admin `), path-traversal upload 400, CSRF documented as 400 (ADR 17).

## Phase 3 — Code quality (18 September 2026)

| Command | Result |
| --- | --- |
| `dotnet test -c Release` | **114 passed**, 0 failed, 2 skipped. |
| Empty `catch` | Removed from `KhidmaApiFactory`; remaining catches log or rethrow. |
| Tests without asserts | None. |

Auth and catalog domain decisions now live in `AuthService` / `CatalogService`. `TreatWarningsAsErrors` is on both csproj files. Query counts: `docs/decisions.md` ADR 18.

## Phase 4 — Frontend quality (18 September 2026)

| Command | Result |
| --- | --- |
| `npx tsc -b` | Passed (`"strict": true` already on in `tsconfig.app.json`; no `any` escapes added). |
| `npm run lint` | Passed. |
| `npm run test` | **29 passed**, 0 failed, 15 files. |
| `npm run build` | Passed. |
| `dotnet test -c Release` | **114 passed**, 0 failed, 2 skipped. |

UI: EmptyState on admin dashboard / user audit / public provider services / document lists; request-form retry; catalog and login field errors; accept 409 shows the server message plus **Reload offers**; request save and offer withdraw use `role="status"`. Submit buttons on mutation forms already use `loading` (disabled while in flight).

A11y: labels, `:focus-visible`, `StatusBadge` text, and `aria-invalid` on mapped field errors. No `dangerouslySetInnerHTML`.

Responsive: `.table-wrap` and `overflow-x: clip` are in place. Live 360/768/1280 screenshots are blocked until SQL Server starts (SPA bootstrap requires `/api/auth/me`). Capture checklist: `docs/screenshots/README.md`. Playwright was not added.

## Phase 5 — Scope justification (18 September 2026)

ADRs 19–21 (verification documents, suspension, audit) and README “Deviations from plan v2”. No product code.

## Phase 6 — Deploy prep (18 September 2026)

| Command | Result |
| --- | --- |
| `dotnet test -c Release` | **117 passed**, 0 failed, 2 skipped. |
| `dotnet ef migrations script --idempotent` | Wrote `deploy/migrate.sql`. |

Production: `CookieSecurePolicy.Always` and `UseHsts()` outside Development/Testing (HSTS also outside Development). `GlobalExceptionHandler` returns a generic 500 with no exception text (`ProductionExceptionTests`). SPA deep link `/customer/requests/1` → `index.html`; `/api/nope` → JSON 404. Azure runbook: `deploy/AZURE_DEPLOY.md`. Cloud create/deploy steps are stopped for you.

## Phase 7 — Docs cleanup (18 September 2026)

Deleted `REVIEW_HANDOFF.md`, `WEEK1_COMPLETION_REPORT.md`, `FINAL_IMPLEMENTATION_REPORT.md`, `docs/ADMIN_SECURITY_REVIEW_REPORT.md`. Folded into `docs/history.md` and `docs/security-matrix.md`. Plan of record: `docs/plan-v2.md`. Seeder adds `provider4@khidma.local` (Ramallah, Approved, Plumbing). Demo script matches §15 with Palestinian cities and 404 for ineligible URLs.

| Command | Result |
| --- | --- |
| `dotnet test -c Release` | **117 passed**, 0 failed, 2 skipped (seeder is Development-only; tests use their own fixtures). |
| Frontend | **29 passed** (unchanged). |
