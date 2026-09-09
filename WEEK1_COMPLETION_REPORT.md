# Week 1 Completion Report

This report is for later review of the Khidma Week 1 authentication vertical slice.

## 1. Starting repository state

- Branch at start: `develop`, matching `origin/develop`
- Working tree: clean
- Recent history:
  - `1553398` Merge pull request #1 from AnasQalalwa/feature/data-layer
  - `d603c7f` feat: add EF Core data layer and initial schema
  - `1fa1a14` chore: scaffold Khidma application
- Days 1–2 were already merged: solution, React Vite client, Identity + EF Core domain/data layer, initial SQL Server migration, idempotent seeder, placeholder xUnit test, GitHub Actions CI
- No authentication API, CSRF, catalog API, React routes, or SPA hosting existed yet

All remaining Week 1 work was done on `feature/week1-auth-integration` created from updated `develop`.

## 2. Files created

Backend:

- `server/Khidma.Api/Auth/AppRoles.cs`
- `server/Khidma.Api/Auth/CurrentUserMapper.cs`
- `server/Khidma.Api/Auth/RegisterResult.cs`
- `server/Khidma.Api/Auth/UserRegistrationService.cs`
- `server/Khidma.Api/Contracts/Auth/RegisterRequest.cs`
- `server/Khidma.Api/Contracts/Auth/LoginRequest.cs`
- `server/Khidma.Api/Contracts/Auth/CurrentUserDto.cs`
- `server/Khidma.Api/Contracts/Catalog/CategoryDto.cs`
- `server/Khidma.Api/Contracts/Catalog/ServiceDto.cs`
- `server/Khidma.Api/Controllers/AuthController.cs`
- `server/Khidma.Api/Controllers/AntiforgeryController.cs`
- `server/Khidma.Api/Controllers/CatalogController.cs`
- `server/Khidma.Api/Infrastructure/GlobalExceptionHandler.cs`
- `server/Khidma.Api/Infrastructure/CookieAuthProblemWriter.cs`
- `server/Khidma.Api.Tests/KhidmaApiFactory.cs`
- `server/Khidma.Api.Tests/AuthEndpointsTests.cs`
- `server/Khidma.Api.Tests/CatalogEndpointsTests.cs`
- `server/Khidma.Api.Tests/AntiforgeryTestHelper.cs`
- `server/Khidma.Api.Tests/AppRolesTests.cs`

Frontend:

- `client/src/api/client.ts`
- `client/src/api/auth.ts`
- `client/src/api/catalog.ts`
- `client/src/auth/AuthContext.tsx`
- `client/src/auth/RequireAuth.tsx`
- `client/src/auth/RequireRole.tsx`
- `client/src/auth/roles.ts`
- `client/src/components/Layout.tsx`
- `client/src/pages/HomePage.tsx`
- `client/src/pages/LoginPage.tsx`
- `client/src/pages/RegisterPage.tsx`
- `client/src/pages/CatalogPage.tsx`
- `client/src/pages/CustomerDashboard.tsx`
- `client/src/pages/ProviderDashboard.tsx`
- `client/src/pages/AdminDashboard.tsx`
- `client/src/pages/ForbiddenPage.tsx`
- `client/src/pages/NotFoundPage.tsx`
- `client/src/styles/tokens.css`
- `client/src/styles/global.css`

Docs:

- `WEEK1_COMPLETION_REPORT.md`

## 3. Files modified

- `server/Khidma.Api/Program.cs`
- `server/Khidma.Api/Data/DbSeeder.cs`
- `server/Khidma.Api/Properties/launchSettings.json`
- `server/Khidma.Api/appsettings.json`
- `server/Khidma.Api.Tests/Khidma.Api.Tests.csproj`
- `client/vite.config.ts`
- `client/package.json`
- `client/package-lock.json`
- `client/index.html`
- `client/src/main.tsx`
- `client/src/App.tsx`
- `.github/workflows/ci.yml`
- `.gitignore`
- `README.md`
- `docs/decisions.md`

Deleted scaffold leftovers:

- `server/Khidma.Api/WeatherForecast.cs`
- `server/Khidma.Api/Controllers/WeatherForecastController.cs`
- `server/Khidma.Api.Tests/UnitTest1.cs`
- `client/src/App.css`
- `client/src/index.css`

## 4. Day 3 work completed

- Identity application cookie: HttpOnly, SameSite=Lax, Secure when HTTPS (`SameAsRequest`), 7-day sliding expiration
- Cookie redirects overridden: login → JSON 401, access denied → JSON 403 (no HTML, no 302)
- Auth DTOs: `RegisterRequest`, `LoginRequest`, `CurrentUserDto`
- `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/logout`, `GET /api/auth/me`
- Public registration limited to Customer/Provider; Admin rejected
- Registration creates Identity user + role + CustomerProfile or ProviderProfile; relational work is transactional on SQL Server
- Successful registration signs the user in (cookie created)
- Invalid login returns generic 401 ProblemDetails
- Antiforgery: `X-XSRF-TOKEN` header, `GET /api/antiforgery/token` issues readable `XSRF-TOKEN` cookie
- Unsafe HTTP methods auto-validated
- ProblemDetails exception handling without stack traces to clients
- Catalog API: categories, services, and services-by-category; `AsNoTracking()` DTOs
- OpenAPI remains Development-only (`AddOpenApi` / `MapOpenApi`)

## 5. Day 4 work completed

- API HTTPS URL: `https://localhost:5001`
- Vite `/api` proxy to that URL with `secure: false` and `Secure` stripped from proxied `Set-Cookie` so HTTP Vite can store cookies
- Production build output: `server/Khidma.Api/wwwroot`, `emptyOutDir: true`
- Single typed fetch wrapper with `credentials: "include"` and CSRF header on POST/PUT/PATCH/DELETE
- Auth and catalog API modules
- `AuthContext` bootstraps via `GET /api/auth/me` before role-dependent chrome
- `RequireAuth` / `RequireRole`
- Routes: `/`, `/login`, `/register`, `/catalog`, `/customer`, `/provider`, `/admin`, `/forbidden`
- Nav: anonymous vs authenticated; only the current role dashboard is shown
- Design tokens + global CSS

## 6. Day 5 work completed

- Home, Register, Login, Catalog, and three role dashboards
- Register form: Customer/Provider only
- Server validation and ProblemDetails shown in the UI
- After register/login, user is signed in and sent to the matching dashboard
- Catalog loads from the API with loading/error/empty states
- ASP.NET Core serves the production SPA (`UseDefaultFiles`, `UseStaticFiles`, `MapFallbackToFile("index.html")`)
- Unknown `/api/*` routes return JSON 404, not `index.html`

## 7. Authentication flow

1. Browser calls `GET /api/antiforgery/token` (also before each mutation).
2. Register or login POST includes `X-XSRF-TOKEN`.
3. Identity issues an HttpOnly cookie (`.AspNetCore.Identity.Application`).
4. React cannot read that cookie. It calls `GET /api/auth/me`.
5. Refresh restores the session because the cookie is sent with `credentials: "include"`.
6. Logout calls `POST /api/auth/logout`, then `/api/auth/me` is 401.

No JWT. No tokens in `localStorage` or `sessionStorage`.

## 8. Cookie configuration

- Authentication cookie: HttpOnly, SameSite=Lax, Secure when the request is HTTPS, 7-day sliding expiration, persistent sign-in
- Identity cookie name: `.AspNetCore.Identity.Application`
- Antiforgery cookie: HttpOnly
- `XSRF-TOKEN`: not HttpOnly, path `/`, SameSite=Lax, Secure when HTTPS

Anonymous `/api/auth/me` returns JSON 401, not HTML.

## 9. CSRF implementation

- `AddAntiforgery(options => options.HeaderName = "X-XSRF-TOKEN")`
- `AutoValidateAntiforgeryTokenAttribute` via `AddControllersWithViews` (required so the filter is registered; `AddControllers` alone does not register it)
- Login/register are not exempt
- Frontend `api/client.ts` fetches a token before every unsafe request so the token stays valid after the user identity changes

## 10. API endpoints

| Method | Path | Auth |
| --- | --- | --- |
| GET | `/api/antiforgery/token` | Anonymous |
| POST | `/api/auth/register` | Anonymous + CSRF |
| POST | `/api/auth/login` | Anonymous + CSRF |
| POST | `/api/auth/logout` | Authenticated + CSRF |
| GET | `/api/auth/me` | Authenticated |
| GET | `/api/catalog/categories` | Anonymous |
| GET | `/api/catalog/services` | Anonymous |
| GET | `/api/catalog/categories/{categoryId}/services` | Anonymous |

OpenAPI is mapped only in Development.

## 11. React routes

- `/` Home
- `/login`
- `/register`
- `/catalog`
- `/customer` — Customer only
- `/provider` — Provider only
- `/admin` — Admin only
- `/forbidden`
- `*` Not found

Frontend guards are UX only. The API remains the security authority.

## 12. Database / seed changes

No schema/model change. No new migration.

Idempotent seeder now targets:

- Roles: Admin, Customer, Provider
- Admin: `admin@khidma.local`
- Customers: `customer@khidma.local` (Ramallah), `customer2@khidma.local` (Nablus)
- Providers: `provider1@khidma.local` (Ramallah), `provider2@khidma.local` (Hebron), `provider3@khidma.local` (Bethlehem)
- Categories (4): Home Services, Technology, Cleaning, Tutoring
- Services (12): Plumbing, Electrical, Painting, Carpentry, Computer Repair, Phone Repair, Network Setup, Home Cleaning, Carpet Cleaning, Window Cleaning, Math Tutoring, English Tutoring
- Providers assigned realistic services
- Passwords still come from `Seed:AdminPassword` and `Seed:DemoPassword`
- Existing users are not recreated; `Ensure*` methods skip existing rows
- Restarting the API a second time did not throw and did not require duplicate inserts

After local smoke registration, SQL inspection showed:

- 3 roles
- 4 categories
- 12 services
- Customer registration: AspNetUsers + AspNetUserRoles + CustomerProfiles (no provider profile)
- Provider registration: AspNetUsers + AspNetUserRoles + ProviderProfiles (no customer profile)

## 13. Error handling

- `AddProblemDetails()`
- `GlobalExceptionHandler` logs the exception and returns a generic 500 ProblemDetails (no stack trace to clients)
- `UseStatusCodePages()` for empty error responses
- Validation → 400
- Unauthenticated → 401
- Authenticated but forbidden → 403
- Missing catalog category → 404
- Duplicate email registration → 409
- Unknown `/api/*` → JSON 404

## 14. Build results

- `dotnet restore` succeeded
- `dotnet build -c Release` succeeded, 0 warnings
- `npx tsc -b` succeeded
- `npm run build` succeeded; output written to `server/Khidma.Api/wwwroot`

## 15. Test results

- `dotnet test -c Release`: 20 passed, 0 failed
- Coverage includes: anonymous `/me` 401 JSON, customer/provider register, admin rejection, invalid login, login/logout, CSRF reject/accept, catalog, persistence of user/role/profile, role normalization
- No frontend unit tests existed; none were added (CI does not require them)

## 16. EF migration / model-change result

```text
dotnet ef migrations has-pending-model-changes
No changes have been made to the model since the last migration.
```

`InitialCreate` was not edited.

## 17. Production SPA hosting result

`npm run build` then `dotnet run --project server/Khidma.Api --launch-profile https` (Vite not running):

- `https://localhost:5001/`
- `/login`
- `/register`
- `/catalog`
- `/customer`
- `/provider`
- `/admin`

all returned the React `index.html`. Direct refresh of those paths works through `MapFallbackToFile`. `/api/does-not-exist` returned JSON 404, not the SPA.

API and SPA share one origin/port: `https://localhost:5001`.

## 18. Manual verification results

Verified with `curl.exe` against the live SQL Server-backed API (and xUnit for in-memory persistence):

1. Anonymous `GET /api/auth/me` → 401 JSON, not HTML, not redirect
2. Valid Customer login → Identity cookie created (HttpOnly, SameSite=Lax)
3. Session restore via `/api/auth/me` with the cookie (browser refresh equivalent)
4. Customer dashboard is a Customer-only React route (`RequireRole`)
5. Provider/Admin dashboards similarly role-gated in the SPA
6. Public Admin registration → 400
7. Logout → subsequent `/api/auth/me` → 401
8. POST without `X-XSRF-TOKEN` → 400
9. Same POST with token → accepted (login/register succeed)
10. Catalog categories/services loaded from the database
11. Seeded Customer, Provider, and Admin logins succeeded
12. Smoke-registered Customer/Provider rows had the matching profile only

Browser DevTools were not available in this environment. SPA route serving was verified over HTTP. Role-dashboard UX redirects were implemented and covered by route-guard design; live click-through in a browser was not possible here.

## 19. Known limitations

- Week 1 dashboards are intentionally thin; request/offer/booking/review workflows are later weeks
- Frontend route guards are UX, not security
- New providers are `IsApproved = true`, matching the existing Day 2 seeder
- `dotnet dev-certs https --trust` may still show a Windows confirmation prompt; `curl -k` and Vite `secure: false` were used for local verification
- Windows PowerShell may block `npm.ps1`; use `npm.cmd`

## 20. Deviations from the capstone plan and why

1. **`AddControllersWithViews` instead of `AddControllers`.** `AutoValidateAntiforgeryTokenAttribute` requires the ViewFeatures filter service. `AddControllers()` threw at runtime: filter type not registered.
2. **LocalDB default in `appsettings.json`.** Connection string is not a password. User-secrets still override it. This avoids a hard fail when configuration sources used by tests/hosting do not include user-secrets, while seed passwords remain secret-only.
3. **Testing host skips SQL Server registration.** CI runs on Ubuntu without LocalDB. Tests use EF InMemory. Program.cs registers SQL Server except when `Environment=Testing`.
4. **CI TypeScript check is `npx tsc -b`.** The previous `tsc --noEmit` against the solution-style `tsconfig.json` (`files: []`) did not typecheck the app. This is stricter, not weaker.
5. **Always refresh antiforgery before mutations.** Identity antiforgery tokens are tied to the current user. Reusing an anonymous token after login/register fails CSRF validation.
6. **No JWT, Docker, payments, or extra UI library**, as specified.
7. **No new EF migration.** Seed expansion did not change the model.

## 21. Git branch and commit hashes

- Branch: `feature/week1-auth-integration`
- Base: `develop` (`1553398`)
- `e6cade8` feat: implement cookie authentication API
- `6e39275` feat: wire React authentication shell
- `4ccbd08` feat: complete week one vertical slice

## 22. Exact instructions for another developer

### Secrets

```bash
dotnet user-secrets set "ConnectionStrings:Default" "<sql-server-connection-string>" --project server/Khidma.Api
dotnet user-secrets set "Seed:AdminPassword" "<admin-password>" --project server/Khidma.Api
dotnet user-secrets set "Seed:DemoPassword" "<demo-password>" --project server/Khidma.Api
```

Never commit the values. Seeded emails: `admin@khidma.local`, `customer@khidma.local`, `customer2@khidma.local`, `provider1@khidma.local`, `provider2@khidma.local`, `provider3@khidma.local`.

### Development (two terminals)

```bash
dotnet run --project server/Khidma.Api --launch-profile https
cd client
npm ci
npm run dev
```

Open the Vite URL. `/api` is proxied to `https://localhost:5001`.

### Production-style local test

```bash
cd client
npm ci
npm run build
cd ..
dotnet run --project server/Khidma.Api --launch-profile https
```

Open `https://localhost:5001` only. Do not start Vite.

### Checks

```bash
dotnet build -c Release
dotnet test -c Release
dotnet ef migrations has-pending-model-changes --project server/Khidma.Api/Khidma.Api.csproj --startup-project server/Khidma.Api/Khidma.Api.csproj
cd client
npx tsc -b
npm run build
```

## Architecture tree

```text
khidma/
├── client/
│   ├── src/
│   │   ├── api/                # typed fetch wrapper, auth, catalog
│   │   ├── auth/               # AuthContext, route guards
│   │   ├── components/         # layout / nav
│   │   ├── pages/             # Home, auth, catalog, dashboards
│   │   └── styles/             # tokens + global CSS
│   └── vite.config.ts         # proxy + wwwroot outDir
├── server/
│   ├── Khidma.Api/
│   │   ├── Auth/               # roles, registration service
│   │   ├── Contracts/           # HTTP DTOs
│   │   ├── Controllers/         # auth, antiforgery, catalog
│   │   ├── Data/                # DbContext, configurations, seeder
│   │   ├── Domain/              # unchanged Week 1 domain
│   │   ├── Infrastructure/      # exception + cookie 401/403
│   │   ├── Migrations/          # InitialCreate only
│   │   └── wwwroot/            # generated SPA, gitignored
│   └── Khidma.Api.Tests/
├── .github/workflows/ci.yml
├── README.md
└── WEEK1_COMPLETION_REPORT.md
```
