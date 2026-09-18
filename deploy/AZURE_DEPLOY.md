# Azure deploy (App Service + Azure SQL)

Khidma is a single ASP.NET Core 10 app: the API and the built SPA (`wwwroot`). Production does **not** auto-migrate or seed.

This file is the runbook. The Azure clicks and `az` commands are yours; stop after the local publish artifacts are ready.

## Target

| Piece | Choice |
| --- | --- |
| App Service | Windows, .NET 10 |
| Database | Azure SQL |
| HTTPS | App Service **HTTPS Only** = On |

## App settings

Set these in App Service Configuration (Application settings). Double-underscore is the Azure form of nested keys.

| Key | Value |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__Default` | Azure SQL connection string (`Encrypt=True;TrustServerCertificate=False`) |
| `Seed__AdminPassword` | Strong admin password (needed only for the one-off seed run below) |
| `Seed__DemoPassword` | Strong demo password (same) |
| `ProviderDocuments__RootPath` | Persistent path, e.g. `D:\home\data\provider-documents` (not `wwwroot`) |

Do not put secrets in source control. Connection strings can also go in **Connection strings** as `Default` (type SQLAzure).

## Build locally

```bash
cd client
npm ci
npm run build
cd ..
dotnet publish server/Khidma.Api -c Release -o artifacts/publish
```

The client build already writes to `server/Khidma.Api/wwwroot`. `dotnet publish` includes that folder.

Zip `artifacts/publish` and deploy with Visual Studio, Azure Portal zip-deploy, or:

```bash
az webapp deploy --resource-group <rg> --name <app> --src-path artifacts/publish.zip --type zip
```

## Schema

1. Create the Azure SQL database.
2. Run `deploy/migrate.sql` with `sqlcmd` or the Query editor **before** the first Production start:

```bash
sqlcmd -S <server>.database.windows.net -d Khidma -U <user> -P <password> -i deploy/migrate.sql
```

Why this is not `Database.MigrateAsync()` in Production: multiple instances race, a failed migration is hard to reverse, and there is no review step (ADR 8).

## Seed (one-off, existing seeder)

There is no `--seed` product switch. After `migrate.sql` succeeds, run the existing Development startup **once** from a trusted machine against Azure SQL:

```bash
dotnet user-secrets set "ConnectionStrings:Default" "<azure-sql>" --project server/Khidma.Api
dotnet user-secrets set "Seed:AdminPassword" "<admin>" --project server/Khidma.Api
dotnet user-secrets set "Seed:DemoPassword" "<demo>" --project server/Khidma.Api
dotnet run --project server/Khidma.Api --launch-profile https --environment Development
```

Confirm `admin@khidma.local` can sign in, then stop the process. App Service stays `ASPNETCORE_ENVIRONMENT=Production` so it will not migrate or seed again.

## Post-deploy smoke

1. `https://<app>.azurewebsites.net/` loads the SPA.
2. `https://<app>.azurewebsites.net/customer/requests/1` (logged out) still returns the SPA, not a JSON 404.
3. `https://<app>.azurewebsites.net/api/nope` is Problem Details 404.
4. Login as admin / customer / `provider1`.
5. Customer creates a Plumbing request in Ramallah; `provider1` sees it; accept → booking.
6. Missing `X-XSRF-TOKEN` on a POST is **400**.
7. Upload a PDF as a provider; another provider cannot download it.
8. Response bodies must not include stack traces.

## STOP

Create the App Service, Azure SQL database, wire the app settings, apply `migrate.sql`, run the one-off seed, and deploy the zip. Do not treat this document as a completed cloud deploy.
