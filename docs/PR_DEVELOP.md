## What changed

Week 4 closeout on `feature/full-project-development`, plus live SQL Server proof on LocalDB 17 after the NVMe sector workaround (ADR 22):

- Both EF migrations applied; `scripts/verify-schema.sql` confirms every §5.3 constraint (filtered unique indexes, `CK_Review_Rating`, rowversion, `decimal(18,2)`, nvarchar enums).
- Live `scripts/concurrency-check.ps1` (provider1 + provider4): **200 + 409**, body `Another offer was accepted first.`, exactly one booking, sibling **Rejected**.
- Live `scripts/smoke-test.ps1` 8/8 and `scripts/security-matrix.ps1` 14/14. CSRF missing token stays **400**.
- `$env:KHIDMA_SQLSERVER_TESTS=1; dotnet test -c Release` → **119 passed**, 0 skipped; default suite still **117 passed**, 2 skipped.
- 360 / 768 / 1280 screenshots in `docs/screenshots/` (Home, Catalog, customer request with offers, provider available requests, booking detail, admin verifications).
- Script robustness only: smoke-test CSRF `$status`; concurrency-check PS7 409 body decode. No product features added.

## Why

Prove the marketplace against the plan v2 rubric on real SQL Server without Docker/Redis/JWT/payments. Keep review-ready evidence in `docs/`.

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

SQL Server (Windows LocalDB): `$env:KHIDMA_SQLSERVER_TESTS=1; dotnet test -c Release`

Demo: `docs/demo-script.md` (Plumbing in Ramallah; Provider B = `provider3` → 404; loser message **Another offer was accepted first.**).

## Screenshots

Key captures: `docs/screenshots/customer-request-detail-1280.png`, `provider-requests-1280.png`, `admin-verifications-1280.png`, `home-360.png`. Full set: `docs/screenshots/README.md`.

## Unsure about

Azure was prepared (`deploy/AZURE_DEPLOY.md`), not deployed. Branch protection and the `v1.0.0` tag wait until this PR is merged to develop, then develop → main. Live UI PDF upload and admin suspend-in-the-browser were not re-run (xUnit covers both). Do not merge this PR from the closeout agent.
