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
