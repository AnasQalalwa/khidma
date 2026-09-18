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
| `gh auth login` | Done. Authenticated as **AnasQalalwa** (HTTPS, `repo` + `workflow`). |
| PR / branch protection / `v1.0.0` | Waiting on the steps below. |
| SQL Server CI job | **Not added.** CI is `ubuntu-latest`; LocalDB is a Windows feature. Opt-in tests stay skipped on the runner. |

### You run next

`gh` is authenticated. The Phase 9 commit opens the PR into `develop` with `docs/PR_DEVELOP.md`. Do **not** merge from this closeout.

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

## Phase 9 — Live SQL Server (18 September 2026)

LocalDB 17.0.4025.3 started after the NVMe sector workaround (ADR 22). User-secrets set on this machine (`ConnectionStrings:Default`, `Seed:AdminPassword`, `Seed:DemoPassword`). Seeder created 7 users including `provider4@khidma.local`.

| Check | Result |
| --- | --- |
| `dotnet ef database update` | Applied `20260904230532_InitialCreate` and `20260916111438_AddProviderVerificationAuditAndSuspension`. Both rows in `__EFMigrationsHistory`. |
| `sqlcmd -i scripts/verify-schema.sql` | All §5.3 objects present. Output pasted in `docs/security-matrix.md`. No new migration. |
| `scripts/smoke-test.ps1` | **8 passed, 0 failed** against `https://localhost:5001`. Fixed a Windows PowerShell parse bug (`var status` → `$status` in the CSRF step). |
| `scripts/concurrency-check.ps1` (provider1 + provider4) | `Accept statuses: 200, 409`. PASS concurrent accept + accept-first message + single booking. PASS sibling `Rejected`. PS7 409 body decode: `SkipHttpErrorCheck` + UTF-8 for `byte[]` Problem Details. |
| `scripts/security-matrix.ps1` | **14 passed, 0 failed**. |
| `$env:KHIDMA_SQLSERVER_TESTS=1; dotnet test -c Release` | **119 passed**, 0 failed, **0 skipped**. |
| Unset `KHIDMA_SQLSERVER_TESTS`; `dotnet test -c Release` | **117 passed**, 0 failed, **2 skipped** (the two SQL Server facts). |
| Screenshots | 360 / 768 / 1280 PNGs in `docs/screenshots/` for Home, Catalog, customer request detail with two pending offers, provider available requests, booking detail, admin verifications. `scrollWidth <= innerWidth` on each. Cookie + CSRF through the Vite proxy (`localhost:5173` → `https://localhost:5001`) worked for customer / provider1 / admin login. |

Concurrency console:

```text
Khidma concurrency check against https://localhost:5001
Accept statuses: 200, 409
PASS  Concurrent accept produced 200 + 409, the accept-first message, and a single booking.
PASS  Sibling offer flipped to Rejected.
```

## Final counts (18 September 2026)

| Suite | Result |
| --- | --- |
| Backend (`dotnet test -c Release`) | **117 passed**, 0 failed, **2 skipped** (SQL Server opt-in, default) |
| Backend with `KHIDMA_SQLSERVER_TESTS=1` | **119 passed**, 0 failed, **0 skipped** |
| Frontend (`npm run test`) | **29 passed**, 0 failed, 15 files |
| SQL Server integration | **2 passed** on LocalDB (`Khidma_Tests`); skipped on Ubuntu CI |

Backend inventory: SQLite `Fact`/`Theory` suite plus `SpaFallbackTests` (2), `ProductionExceptionTests` (1), and two `SqlServerIntegrationTests` (opt-in).

## Rubric (§13) — evidence and estimated score

| # | Criterion | Pts | Evidence | Est. |
| --- | --- | --- | --- | --- |
| 1 | Workflow correctness | 25 | 11 MVP items covered by xUnit. Live SQL race: 200 + 409 `Another offer was accepted first.`, one booking, sibling Rejected (`concurrency-check.ps1` + `SqlServerIntegrationTests`). | 25 |
| 2 | Data model & EF Core | 15 | Two migrations applied. Live `sys.indexes` dump in `docs/security-matrix.md`: filtered UX_Offer_OneAcceptedPerRequest, filtered non-withdrawn offer uniqueness, unique Review.BookingId / Booking.OfferId / ProviderService, CK_Review_Rating, rowversion, decimal(18,2), nvarchar enums. | 15 |
| 3 | Security & authorization | 15 | Matrix + live `security-matrix.ps1` 14/14. CSRF 400; admin register rejected; ineligible URL 404. Magic-byte / oversize / peer-download remain xUnit. | 14 |
| 4 | Backend code quality | 12 | Auth/catalog in services; `CallerRole`; `TreatWarningsAsErrors`; dashboard query counts ADR 18 (source counts; live `Executed DbCommand` tally not recaptured). | 11 |
| 5 | Frontend quality | 13 | `strict: true`; EmptyState/retry; field errors; 409 + Reload offers; 29 Vitest cases. Live 360/768/1280 PNGs in `docs/screenshots/`. | 13 |
| 6 | Testing | 10 | 117 + 29 default; 119 with SQL opt-in; CI Backend/Frontend; no assert-less tests. | 10 |
| 7 | Git & process | 10 | Conventional commits; CI from week 1; `gh` authenticated; PR into develop in this closeout. Branch protection and `v1.0.0` tag still operator steps. | 9 |
| | **Subtotal** | **100** | | **97** |
| Bonus | ADRs, deploy prep | +5 | `docs/decisions.md` ADRs 1–22; `deploy/migrate.sql` + `AZURE_DEPLOY.md`. App is **not** live on Azure. | +3 |

Estimated **~100 / 100** on product evidence. Remaining points are operator: Azure hosting, branch protection, tag.

## Numbered manual / remaining checklist

1. **Done — SQL Server LocalDB** — instance starts; ADR 22 + README NVMe note.
2. **Done — user-secrets** — `ConnectionStrings:Default`, `Seed:AdminPassword`, `Seed:DemoPassword`; both migrations applied; seeder ran.
3. **Done — schema proof** — `scripts/verify-schema.sql` output in `docs/security-matrix.md`.
4. **Done — live smoke** — 8/8.
5. **Done — live concurrency** — 200 + 409, accept-first message, one booking, sibling Rejected.
6. **Done — live security matrix** — 14/14.
7. **Done — `KHIDMA_SQLSERVER_TESTS=1`** — 2/2 passed (0 skipped).
8. **Done — screenshots** — 18 PNGs in `docs/screenshots/` (360/768/1280 × 6 pages).
9. **Manual — Azure** — follow `deploy/AZURE_DEPLOY.md` (App Service Windows .NET 10 + Azure SQL). Do not auto-migrate.
10. **Done — GitHub CLI login** — `AnasQalalwa`. PR into develop is this closeout; do not merge here.
11. **After merge to develop** — `gh pr create --base main --head develop`.
12. **Branch protection** — Settings → Branches: PR required, checks **Backend** + **Frontend**, 1 approval, no force-push, on `main` and `develop`.
13. **After merge to main** — annotated tag `v1.0.0` and push.
14. **Rehearse** — `docs/demo-script.md` twice on a running API (local or Azure).
15. **Manual — admin UI at extra widths** — 430 / 1024 / 1440 (360/768/1280 captured).
16. **Manual — real PDF upload** — provider profile upload + admin download.
17. **Manual — suspend in the UI** — confirm pending offers flip to Rejected (covered by `SuspensionTests`).
18. **Manual — live dashboard query counts** — ADR 18 source counts; `Executed DbCommand` capture on SQL Server not repeated in Phase 9.
