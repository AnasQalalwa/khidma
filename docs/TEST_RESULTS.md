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
