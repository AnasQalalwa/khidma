## What changed

Week 4 closeout on `feature/full-project-development`: unified accept-race 409, SQL Server opt-in tests, security-matrix walk (CSRF stays 400), thin auth/catalog services, frontend field errors and 409 reload, ADRs 19–21, `deploy/migrate.sql` + Azure runbook, docs cleanup, seeded `provider4` for the two-offer demo.

## Why

Prove the marketplace against the plan v2 rubric without new product features (no Docker/Redis/JWT/payments). Keep review-ready evidence in `docs/`.

## How to test

```powershell
dotnet build -c Release -warnaserror
dotnet test -c Release
cd client
npm run lint
npx tsc -b
npm run test
npm run build
```

SQL Server (Windows, after LocalDB starts): `$env:KHIDMA_SQLSERVER_TESTS=1; dotnet test -c Release --filter FullyQualifiedName~SqlServerIntegrationTests`

Demo: `docs/demo-script.md` (Plumbing in Ramallah; Provider B = `provider3` → 404; loser message **Another offer was accepted first.**).

## Screenshots

Live 360/768/1280 captures are blocked until SQL Server starts (`docs/screenshots/README.md`).

## Unsure about

LocalDB on this NVMe host crashes (`256 misaligned log IOs`). Live schema/smoke/concurrency/security-matrix were not run. Azure was prepared, not deployed. `gh auth login` is still required to open this PR from the CLI if you paste the body yourself.
