# Khidma

Khidma is a local service marketplace. Customers publish service requests, eligible providers submit offers, the customer accepts exactly one offer, and the resulting booking is started, completed, and reviewed.

## Workflow

```text
Customer creates ServiceRequest (Open)
        │
        ▼
Eligible provider sees it (verified + not suspended + matching service + same city)
        │
        ▼
Provider submits Offer (Pending)
        │
        ▼
Customer accepts one offer (transaction)
        │  siblings → Rejected, request → Booked, Booking → Scheduled
        ▼
Provider starts (InProgress) then completes (Completed + request Completed)
        │
        ▼
Customer leaves one review (rating 1–5)
        │
        ▼
ProviderProfile.AverageRating / ReviewCount recomputed in the same transaction
```

Roles: **Customer**, **Provider**, **Admin**. New providers register as `PendingReview`. They become eligible only after an admin approves at least one professional document and the provider account, and only while the account is not suspended.

Out of scope: payments, chat, JWT, maps, notifications, and multi-offer acceptance.

## Technology stack

| Layer | Stack |
| --- | --- |
| API | ASP.NET Core 10, EF Core 10, SQL Server, Identity cookie auth + CSRF |
| Client | React 19, TypeScript, Vite, React Router |
| Tests | xUnit + WebApplicationFactory (SQLite in-memory); Vitest + Testing Library |
| CI | GitHub Actions (`.github/workflows/ci.yml`) |

## Repository layout

```text
khidma/
├── client/                 React app
├── server/
│   ├── Khidma.Api/         Web API + SPA host (wwwroot)
│   └── Khidma.Api.Tests/
├── docs/                   ADRs, security matrix, checklists
├── .config/dotnet-tools.json
├── Khidma.sln
└── README.md
```

## User secrets

Do not commit passwords or connection strings. `appsettings.json` has no `ConnectionStrings`. Set these on `server/Khidma.Api`:

- `ConnectionStrings:Default`
- `Seed:AdminPassword`
- `Seed:DemoPassword`

```bash
dotnet user-secrets set "ConnectionStrings:Default" "<sql-server-connection-string>" --project server/Khidma.Api
dotnet user-secrets set "Seed:AdminPassword" "<admin-password>" --project server/Khidma.Api
dotnet user-secrets set "Seed:DemoPassword" "<demo-password>" --project server/Khidma.Api
```

Typical LocalDB:

`Server=(localdb)\\mssqllocaldb;Database=Khidma;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True`

### Seeded accounts (emails only)

Passwords come from `Seed:*` secrets, never from source.

| Email | Role | Notes |
| --- | --- | --- |
| `admin@khidma.local` | Admin | Catalog, verification, suspension, users, audit |
| `customer@khidma.local` | Customer | Ramallah — full-flow pair with provider 1 |
| `customer2@khidma.local` | Customer | Nablus |
| `provider1@khidma.local` | Provider | Ramallah, **Approved**, Home Services |
| `provider2@khidma.local` | Provider | Hebron, **PendingReview** |
| `provider3@khidma.local` | Provider | Bethlehem, **Approved** |

## Development

Two terminals. Vite proxies `/api` to `https://localhost:5001`.

### API

```bash
dotnet restore
dotnet tool restore
dotnet run --project server/Khidma.Api --launch-profile https
```

Development startup applies EF migrations and the idempotent seeder. Trust the HTTPS certificate if asked:

```bash
dotnet dev-certs https --trust
```

### React

```bash
cd client
npm ci
npm run dev
```

On Windows PowerShell, if `npm` is blocked, use `npm.cmd`. Open the Vite URL (typically `http://localhost:5173`).

### Production-style local host

```bash
cd client
npm ci
npm run build
cd ..
dotnet run --project server/Khidma.Api --launch-profile https
```

Browse `https://localhost:5001`. ASP.NET Core serves the SPA from `wwwroot` with fallback to `index.html`. `/api/*` misses return Problem Details 404, not the SPA.

## Commands

```bash
# Backend
dotnet restore
dotnet build -c Release
dotnet test -c Release

# Frontend (from client/)
npm ci
npm run lint
npx tsc -b
npm run test
npm run build

# EF (after dotnet tool restore)
dotnet ef migrations list --project server/Khidma.Api
dotnet ef migrations has-pending-model-changes --project server/Khidma.Api
dotnet ef database update --project server/Khidma.Api
dotnet ef migrations script --idempotent --project server/Khidma.Api --output khidma.sql

# Live API checks (never print secrets; passwords from env or SecureString prompts)
pwsh -File scripts/smoke-test.ps1
pwsh -File scripts/concurrency-check.ps1
```

Production does **not** auto-migrate. Apply the idempotent script (or `dotnet ef database update`) as a deploy step.

Migrations: `InitialCreate`, then `AddProviderVerificationAuditAndSuspension` (converts `IsApproved` to `VerificationStatus`, adds documents, suspension, audit logs, and `LastLoginAt`).

## Architecture notes

- Thin controllers → services → EF. Responses are DTOs only.
- `ServiceResult<T>` maps to 400 / 401 / 403 / 404 / 409 Problem Details.
- Lists are paged (`page`, `pageSize`, max 50).
- Eligibility is one query reused by available-list and provider detail: `VerificationStatus == Approved && !IsSuspended`, plus matching service and exact city.
- Unverified or suspended providers get **200 empty** on `GET /api/service-requests/available` and **403** on `POST .../offers`.
- Professional verification documents (PDF/JPEG/PNG, 10 MB, magic-byte check) are stored outside `wwwroot` under `App_Data/provider-documents`.
- Admin workspace: Overview, Users, Provider Verification, Providers (suspend/reactivate), Audit Logs, Catalog.
- Audit logs are append-only. There is no update or delete API.
- Accept-offer is transactional with state checks, rowversion, and a filtered unique index.
- Identity user id vs `ProviderProfile.Id`: see `docs/decisions.md` ADR 11.

## Limitations

- City match is exact (case-insensitive), not geographic.
- SQLite tests cannot `ORDER BY DateTimeOffset`; lists sort by `Id`.
- SQLite filtered indexes are not SQL Server. Concurrent-accept proof on SQL Server is still a manual check.
- No payments, messaging, or email sending. File uploads are limited to private professional verification documents.
- Rating aggregates are stored; they are recomputed only when a review is created (reviews are immutable after insert).

## Docs

- `docs/decisions.md` — ADRs
- `docs/security-matrix.md`
- `docs/final-test-checklist.md`
- `docs/demo-script.md`
- `docs/TEST_RESULTS.md`
- `docs/ADMIN_SECURITY_REVIEW_REPORT.md`
- `FINAL_IMPLEMENTATION_REPORT.md`
