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

## Phase 8 — Git & release (18 September 2026)

| Step | Result |
| --- | --- |
| `winget install GitHub.cli` | Installed **2.101.0** (user scope). |
| `git push -u origin HEAD` | `origin/feature/full-project-development` |
| `gh auth login` | **STOP.** `gh` is not logged in. |
| PR / branch protection / `v1.0.0` | Waiting on the steps below. |
| SQL Server CI job | **Not added.** CI is `ubuntu-latest`; LocalDB is a Windows feature. Opt-in tests stay skipped on the runner. |

### You run next

```powershell
# New shell so PATH includes gh
gh auth login
# GitHub.com → HTTPS → login with a web browser

gh pr create --base develop --head feature/full-project-development --title "Week 4 closeout: SQL-ready accept race, security matrix, deploy artifacts" --body "$(Get-Content -Raw docs/PR_DEVELOP.md)"
gh run watch
```

After that PR is merged:

```powershell
gh pr create --base main --head develop --title "Release v1.0.0" --body "Promote develop to main for v1.0.0."
```

GitHub → **Settings → Branches** for **`main`** and **`develop`**:

1. Add a branch protection rule.
2. Require a pull request before merging.
3. Require status checks: **Backend**, **Frontend**.
4. Require 1 approval.
5. Do not allow force pushes.

After merge to `main`:

```powershell
git checkout main
git pull
git tag -a v1.0.0 -m "Khidma marketplace v1.0.0"
git push origin v1.0.0
```

Azure cloud create/deploy remains stopped at `deploy/AZURE_DEPLOY.md`.

## Final counts (18 September 2026)

| Suite | Result |
| --- | --- |
| Backend (`dotnet test -c Release`) | **117 passed**, 0 failed, **2 skipped** (SQL Server opt-in) |
| Frontend (`npm run test`) | **29 passed**, 0 failed, 15 files |
| SQL Server integration | 2 tests present; skipped unless `KHIDMA_SQLSERVER_TESTS=1` |

Backend inventory: SQLite `Fact`/`Theory` suite plus `SpaFallbackTests` (2), `ProductionExceptionTests` (1), and two skipped `SqlServerIntegrationTests`.

## Rubric (§13) — evidence and estimated score

| # | Criterion | Pts | Evidence | Est. |
| --- | --- | --- | --- | --- |
| 1 | Workflow correctness | 25 | 11 MVP items covered by xUnit (`ServiceRequestTests`, `OfferTests`, `AcceptOfferTests`, `BookingTests`, `ReviewTests`, `FullMarketplaceWorkflowTests`). Accept 409 unified. Live SQL race **not** proven (LocalDB crash). | 22 |
| 2 | Data model & EF Core | 15 | Two migrations; money `decimal(18,2)`; rowversion; filtered unique indexes in model. Live `sys.indexes` dump **not** captured. | 13 |
| 3 | Security & authorization | 15 | Matrix in `docs/security-matrix.md`; CSRF 400; admin register rejected; documents magic-byte/path tests; no live secret in history. Live HTTP script blocked. Ineligible URL is 404 (chosen). | 13 |
| 4 | Backend code quality | 12 | Auth/catalog in services; `CallerRole`; `TreatWarningsAsErrors`; dashboard query counts ADR 18 (no N+1 found). | 11 |
| 5 | Frontend quality | 13 | `strict: true`; EmptyState/retry; field errors; 409 + Reload offers; 29 Vitest cases. Live 360/768/1280 PNGs blocked. | 11 |
| 6 | Testing | 10 | 117 + 29 automated; CI Backend/Frontend; SQL opt-in honest skip; no assert-less tests. | 9 |
| 7 | Git & process | 10 | Conventional commits across weeks; CI from week 1; branch pushed. PR, protection, and tag need your `gh auth login`. | 8 |
| | **Subtotal** | **100** | | **87** |
| Bonus | ADRs, deploy prep | +5 | `docs/decisions.md` ADRs 1–21; `deploy/migrate.sql` + `AZURE_DEPLOY.md`. App is **not** live on Azure. | +3 |

Estimated **~90 / 100** once you merge, protect branches, and (optionally) get LocalDB running. Do not treat SQL Server rows as VERIFIED until the sector workaround + reboot.

## Numbered manual / blocked checklist

1. **Blocked — SQL Server LocalDB** — `256 misaligned log IOs`. Registry `ForcedPhysicalSectorSizeInBytes` = `* 4095`, reboot, then `sqllocaldb start MSSQLLocalDB`. Details: `docs/security-matrix.md`.
2. **Blocked — user-secrets on this machine** — set `ConnectionStrings:Default`, `Seed:AdminPassword`, `Seed:DemoPassword` after LocalDB starts; `dotnet ef database update`.
3. **Blocked — schema proof** — `sqlcmd -i scripts/verify-schema.sql`; paste output into `docs/security-matrix.md`.
4. **Blocked — live smoke** — `pwsh -File scripts/smoke-test.ps1` against `--launch-profile https`.
5. **Blocked — live concurrency** — `pwsh -File scripts/concurrency-check.ps1` with `KHIDMA_PROVIDER2_*` (`provider4`).
6. **Blocked — live security matrix** — `pwsh -File scripts/security-matrix.ps1`.
7. **Blocked — `KHIDMA_SQLSERVER_TESTS=1`** — two tests still skipped.
8. **Blocked — screenshots** — 360/768/1280 PNGs in `docs/screenshots/` once `/api/auth/me` works.
9. **Blocked — Azure** — follow `deploy/AZURE_DEPLOY.md` (App Service Windows .NET 10 + Azure SQL). Do not auto-migrate.
10. **STOP — GitHub CLI login** — `gh auth login`, then `gh pr create --base develop` (body: `docs/PR_DEVELOP.md`).
11. **After merge to develop** — `gh pr create --base main --head develop`.
12. **Branch protection** — Settings → Branches: PR required, checks **Backend** + **Frontend**, 1 approval, no force-push, on `main` and `develop`.
13. **After merge to main** — annotated tag `v1.0.0` and push.
14. **Rehearse** — `docs/demo-script.md` twice on a running API (local or Azure).

