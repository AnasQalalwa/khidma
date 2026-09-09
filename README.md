# Khidma

Khidma is a service booking marketplace where customers create service requests, eligible providers submit offers, customers accept one offer, and the resulting booking is managed through completion and review.

## Project Goal

Build a complete service marketplace workflow while applying professional software engineering practices including:

- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- React
- TypeScript
- Authentication and authorization
- Business-rule enforcement
- Automated testing
- Git and GitHub workflow
- CI/CD fundamentals

## Technology Stack

### Backend
- C#
- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- ASP.NET Core Identity (cookie authentication, not JWT)

### Frontend
- React 19
- TypeScript
- Vite
- React Router
- CSS

### Testing
- xUnit
- WebApplicationFactory

### Development
- Git
- GitHub
- GitHub Actions

## Repository Structure

```text
khidma/
├── client/
├── server/
│   ├── Khidma.Api/
│   └── Khidma.Api.Tests/
├── docs/
├── .github/
│   └── workflows/
├── .gitignore
├── README.md
└── Khidma.sln
```

## User secrets

Do not commit passwords or connection strings. Set these keys with `dotnet user-secrets` on `server/Khidma.Api`:

- `ConnectionStrings:Default`
- `Seed:AdminPassword`
- `Seed:DemoPassword`

Example (replace the values locally; never commit them):

```bash
dotnet user-secrets set "ConnectionStrings:Default" "<sql-server-connection-string>" --project server/Khidma.Api
dotnet user-secrets set "Seed:AdminPassword" "<admin-password>" --project server/Khidma.Api
dotnet user-secrets set "Seed:DemoPassword" "<demo-password>" --project server/Khidma.Api
```

A typical local SQL Server LocalDB connection string looks like:

`Server=(localdb)\\mssqllocaldb;Database=Khidma;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True`

Seeded development accounts (passwords come from user-secrets, not from source):

- Admin: `admin@khidma.local`
- Customers: `customer@khidma.local`, `customer2@khidma.local`
- Providers: `provider1@khidma.local`, `provider2@khidma.local`, `provider3@khidma.local`

## Development

Use two terminals. The React dev server proxies `/api` to `https://localhost:5001`.

### API

```bash
dotnet restore
dotnet run --project server/Khidma.Api --launch-profile https
```

The API listens on `https://localhost:5001` (and `http://localhost:5000`). Development startup applies EF migrations and runs the idempotent seeder.

Trust the ASP.NET HTTPS development certificate if the browser or Vite proxy warns about it:

```bash
dotnet dev-certs https --trust
```

### React

```bash
cd client
npm ci
npm run dev
```

On Windows PowerShell, if `npm` is blocked by the execution policy, use `npm.cmd` instead.

Open the Vite URL (typically `http://localhost:5173`). The browser should talk to `/api` on the same origin; Vite forwards those calls to the API.

## Production-style local test

Build the React app into `server/Khidma.Api/wwwroot`, then run only the API (do not start Vite):

```bash
cd client
npm ci
npm run build
cd ..
dotnet run --project server/Khidma.Api --launch-profile https
```

Browse `https://localhost:5001`. Routes such as `/`, `/login`, `/register`, `/catalog`, and the role dashboards are served by ASP.NET Core with SPA fallback. Refreshing those URLs must still return the React app. API routes remain under `/api`.

## Authentication notes

- Identity cookies are HttpOnly. React never reads an authentication token from storage.
- React restores the session with `GET /api/auth/me`.
- Unsafe requests send `X-XSRF-TOKEN` after `GET /api/antiforgery/token`.
- Public registration allows only Customer and Provider. Admin is seeded, not publicly registered.
