# Service Booking Platform — "Khidma"

## Capstone Project Plan v2 · React SPA + ASP.NET Core Web API · 4 Weeks

> **Project goal.** Build a working service marketplace end to end — customers post service
> requests, providers bid with offers, the customer accepts exactly one, a booking is created and
> worked to completion, and the provider gets reviewed. The point is not the feature list; it is
> that every rule in the workflow is enforced by code, guarded by tests, and defensible in a
> code review.

---

## 0. What changed from v1, and why

| v1 | v2 | Reason |
|---|---|---|
| HTML/CSS + Razor views, ASP.NET Core **MVC** | **React 19 + Vite + TypeScript** SPA against an **ASP.NET Core Web API** | Requested. Razor views and a React SPA are two competing UIs — you pick one. Controllers now return JSON, not `IActionResult` views. |
| 8 weeks / 8 phases | **4 weeks**, vertical-slice-first | Requested. See the honesty note below. |
| "React — avoid in v1" | React is the frontend | Superseded by the new requirement. The rest of the avoid-list (microservices, Docker, Redis, ML, payments) **still stands**. |
| Rules stated as prose | Explicit **state machines**, an authorization matrix, and DB-level constraints | Prose rules produce guessed implementations. §6–§8 remove the guessing. |
| Testing in week 8, "xUnit later" | Service layer + tests from **week 2** | Tests written after the code is written in a 4-week project means no tests. |
| Photos / approval / complaints in both MVP **and** V2 | Resolved, one side each (§2) | v1 contradicted itself in three places. |
| No rubric | **Grading rubric** (§13) | It's a graded capstone. The intern should know what's scored. |

### Honesty note — read this before committing to the timeline

v1 allocated 8 weeks to .NET fundamentals alone. v2 halves that **and** adds React, TypeScript,
and an HTTP boundary. That is a real increase in surface area, not a reshuffle.

This plan is built for:

- **Full-time**, roughly **35–40 h/week ≈ 150 h total**.
- An intern with **prior programming exposure** — variables, functions, loops, basic OOP, some
  Git. It is not a from-zero plan.
- **A mentor available for ~2 h/week** (one code review + one demo).

Scope has been cut to fit (§2), and §14 is an ordered **cut list** for when — not if — a week
runs long. If the intern is a true beginner or part-time, use v1's 8-week schedule with this
document's technical content instead of compressing.

---

## 1. Architecture

```
┌──────────────────────────────┐          ┌─────────────────────────────────┐
│  React 19 + Vite + TS        │          │  ASP.NET Core 10 Web API        │
│  client/                     │  fetch   │  ServiceBooking.Api/            │
│                              │  JSON    │                                 │
│  pages/    routes            │ ───────► │  Controllers/   thin, HTTP only │
│  api/      typed fetch client│          │  Services/      ALL rules here  │
│  context/  AuthContext       │ ◄─────── │  Data/          EF Core context  │
│  components/                 │  cookie  │  Dtos/          request/response │
└──────────────────────────────┘  session └─────────────────────────────────┘
                                                          │
                                                   EF Core │ migrations
                                                          ▼
                                              ┌────────────────────────┐
                                              │  SQL Server            │
                                              │  (LocalDB dev /        │
                                              │   Azure SQL deployed)  │
                                              └────────────────────────┘

dev:  Vite :5173  ──proxy /api──►  API :5001        (same-origin, no CORS)
prod: React built into API's wwwroot, one deployable, SPA fallback route
```

**Two rules that hold this architecture together. Everything else is detail.**

1. **Controllers contain no business logic.** A controller parses input, calls one service
   method, maps the result to an HTTP status. If a controller has an `if` about the domain, it
   belongs in `Services/`. This is what makes the project testable in week 2 instead of
   untestable in week 4.
2. **The server is the only authority.** Client-side validation and hidden buttons are UX. Every
   rule is re-checked server-side, including *"is this row yours?"*.

---

## 2. Scope decisions (this replaces v1's contradictions)

v1 listed three features in both the MVP and the V2 backlog. Resolved:

| Feature | v1 status | **v2 decision** | Rationale |
|---|---|---|---|
| Request photo uploads | MVP entity `RequestImage` **and** V2 | **Cut from MVP.** No `RequestImage` entity. | File upload done properly means content-type sniffing, size limits, path-traversal defence, and storage config. That's 1–2 days of security work that teaches nothing about the core workflow. |
| Provider approval | MVP entity field **and** V2 | **In MVP, minimal.** `ProviderProfile.IsApproved`, default `true`, admin can toggle. Unapproved providers can't see requests or send offers. | Costs one column and one endpoint, and it's load-bearing for the eligibility rule. |
| Complaints | Admin duty **and** V2 | **Cut entirely.** | Whole extra moderation domain. |
| Notifications | "optional later" entity | **Cut entirely.** No entity, no table. | Needs email/SignalR. Out of scope. |
| Admin dashboard | Full: users, block/suspend, moderate, stats | **Reduced:** category/service CRUD + provider approval toggle + a 4-number stat strip. No block/suspend, no review moderation. | Keeps the admin role real without a second app. |
| Favorites, search/filters, portfolio, maps, payments | V2–V4 | **Unchanged — out of scope.** | |

### Explicit non-goals

No payments · no email or SMS · no real-time push · no file uploads · no maps or geocoding · no
mobile app · no Docker/Kubernetes · no microservices · no caching layer · no i18n · no
public/anonymous browsing beyond the catalog and home page.

### The MVP — done means all 11 of these work without touching the database by hand

1. Register and log in as Customer, Provider, or Admin; a session survives a page refresh.
2. Admin seeds/edits categories and services; admin approves providers.
3. A provider builds a profile: city, years of experience, and the services they offer.
4. A customer creates a service request (service, title, description, city, preferred date,
   optional budget range).
5. A provider sees **only eligible** open requests (§7.4) and never a customer's contact details
   before acceptance.
6. A provider submits an offer (price, message, estimated date) and may withdraw it while pending.
7. The customer sees offers on their own request and accepts **exactly one** — concurrency-safe.
8. Accepting atomically: marks that offer `Accepted`, all siblings `Rejected`, the request
   `Booked`, and creates the `Booking`.
9. The provider advances the booking `Scheduled → InProgress → Completed`; illegal transitions
   are rejected with a 409.
10. The customer leaves exactly one review on a `Completed` booking; a second attempt fails.
11. Each role lands on its own dashboard and cannot reach another role's data by URL or API.

---

## 3. Technology stack

| Area | Choice | Notes |
|---|---|---|
| Runtime | **.NET 10 (LTS)** | Current LTS. `dotnet --version` should read `10.x`. |
| API | ASP.NET Core Web API, controller-based | Controllers over minimal APIs: attribute routing and filters are what the intern will meet in real codebases. |
| ORM | EF Core 10, code-first migrations | |
| Database | SQL Server — LocalDB or Developer Edition locally, Azure SQL deployed | LocalDB avoids the Docker ban. |
| Auth | **ASP.NET Core Identity + cookie authentication** | See §8 for why cookies and not a JWT in `localStorage`. |
| Frontend | **React 19 + Vite + TypeScript** | `npm create vite@latest client -- --template react-ts` |
| Routing | **React Router v7** (library mode) | |
| Data fetching | **Plain `fetch`** behind one typed client + custom hooks | TanStack Query is the professional answer and is listed as optional enrichment — but writing the loading/error states by hand once is worth more here than importing the abstraction on day 3. |
| Styling | **CSS Modules** + one `tokens.css` of custom properties | Preserves v1's CSS-fundamentals objective. Tailwind is an acceptable substitute; a component library is not — you'd learn the library, not CSS. |
| Forms | Controlled components + server `ValidationProblemDetails` | React Hook Form + Zod optional. |
| API tests | **xUnit** + `WebApplicationFactory` | |
| Unit test DB | **SQLite in-memory** (see the caveat in §11) | *Not* EF Core InMemory. |
| Frontend tests | **Vitest** + React Testing Library, ~5 tests | |
| CI | **GitHub Actions** — build + test on every PR | |
| Deploy | Azure App Service (Windows) + Azure SQL, single artifact | |

### Still on the avoid list

Microservices · Docker/Kubernetes · Redis or message queues · ML/AI features · real payment
integration · gRPC/GraphQL · Blazor · a component library (MUI/AntD) · a state-management
library (Redux/Zustand) — React Context is enough at this size.

---

## 4. Repository layout

Single repo, two apps, one solution.

```
khidma/
├─ .github/workflows/ci.yml
├─ .gitignore                          # dotnet + node, and appsettings.*.Local.json
├─ README.md
├─ docs/
│  ├─ decisions.md                     # 1 short ADR per real choice
│  └─ demo-script.md
├─ Khidma.sln
├─ server/
│  ├─ Khidma.Api/
│  │  ├─ Controllers/                  # thin: Auth, Catalog, Providers, ServiceRequests,
│  │  │                                #       Offers, Bookings, Reviews, Admin, Antiforgery
│  │  ├─ Services/                     # ALL business rules live here
│  │  │  ├─ IServiceRequestService.cs / ServiceRequestService.cs
│  │  │  ├─ IOfferService.cs           / OfferService.cs          # accept-offer transaction
│  │  │  ├─ IBookingService.cs         / BookingService.cs        # status state machine
│  │  │  └─ IReviewService.cs          / ReviewService.cs
│  │  ├─ Data/
│  │  │  ├─ AppDbContext.cs
│  │  │  ├─ Configurations/            # one IEntityTypeConfiguration<T> per entity
│  │  │  └─ DbSeeder.cs                # roles, admin user, categories, services
│  │  ├─ Domain/                       # entities + enums
│  │  ├─ Dtos/                         # request/response records — never expose entities
│  │  ├─ Common/                       # Result<T>, DomainError, exception handler
│  │  ├─ Migrations/
│  │  ├─ wwwroot/                      # build output (gitignored)
│  │  ├─ Program.cs
│  │  └─ appsettings.json              # NO secrets. Connection string via user-secrets.
│  └─ Khidma.Api.Tests/
│     ├─ Services/                     # unit tests, SQLite in-memory
│     └─ Api/                          # integration tests, WebApplicationFactory
└─ client/
   ├─ src/
   │  ├─ api/          client.ts (fetch wrapper, CSRF header), types.ts, endpoints/
   │  ├─ auth/         AuthContext.tsx, RequireAuth.tsx, RequireRole.tsx
   │  ├─ components/   Button, Field, StatusBadge, EmptyState, ErrorBanner, Spinner
   │  ├─ pages/        auth/ customer/ provider/ admin/
   │  ├─ hooks/        useApi.ts, useRequests.ts, useOffers.ts
   │  ├─ styles/       tokens.css, global.css
   │  ├─ App.tsx       router
   │  └─ main.tsx
   ├─ vite.config.ts   proxy + build.outDir → ../server/Khidma.Api/wwwroot
   ├─ tsconfig.json
   └─ package.json
```

---

## 5. Data model

Nine entities. Every relationship cardinality the syllabus wants shows up naturally.

| Entity | Purpose |
|---|---|
| `ApplicationUser : IdentityUser` | Login identity shared by all roles. Adds `FullName`, `PhoneNumber`, `CreatedAt`. |
| `CustomerProfile` | 1:1 with a customer user. City, default contact. |
| `ProviderProfile` | 1:1 with a provider user. City, `YearsOfExperience`, `Bio`, `IsApproved`, `AverageRating`, `ReviewCount`. |
| `Category` | "Home Services", "Technology". |
| `Service` | "Electrician", "AC Repair". Belongs to a category. |
| `ProviderService` | **Join table** — which services a provider offers. |
| `ServiceRequest` | A customer's request for one service. |
| `Offer` | A provider's bid on a request. |
| `Booking` | Created from the accepted offer. The unit of work being tracked. |
| `Review` | One per completed booking. |

### 5.1 Relationships

```
Category   1 ──< Service
Service    1 ──< ServiceRequest
Service    1 ──< ProviderService >── 1 ProviderProfile      (many-to-many)
Customer   1 ──< ServiceRequest
ServiceRequest 1 ──< Offer
Provider   1 ──< Offer
Offer      1 ──1 Booking                                    (1:1, optional until accepted)
Booking    1 ──0..1 Review                                  (1:1, optional)
Booking    1 ──1 ServiceRequest / Customer / Provider        (denormalized, see below)
```

### 5.2 Core fields

`Booking` and `Review` intentionally duplicate `CustomerId` / `ProviderId`, which are reachable
through the offer chain. **This is a deliberate denormalization** — the dashboards read
"my bookings" constantly and the alternative is a three-table join on every page. Write it in
`docs/decisions.md` as a conscious trade-off with its cost (two places to keep consistent), not
as an accident. Being able to defend it is part of the grade.

```csharp
// Domain/ServiceRequest.cs
public class ServiceRequest
{
    public int Id { get; set; }
    public string CustomerId { get; set; } = default!;   // ApplicationUser.Id
    public int ServiceId { get; set; }
    public string Title { get; set; } = default!;        // required, <= 120
    public string Description { get; set; } = default!;  // required, <= 2000
    public string City { get; set; } = default!;         // required, <= 80
    public DateTimeOffset PreferredDate { get; set; }    // must be in the future
    public decimal? BudgetMin { get; set; }              // decimal(18,2)
    public decimal? BudgetMax { get; set; }              // >= BudgetMin when both present
    public ServiceRequestStatus Status { get; set; }     // Open | Booked | Completed | Cancelled
    public DateTimeOffset CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = default!;   // concurrency guard for accept-offer
}
```

| Model | Fields |
|---|---|
| `Offer` | `Id`, `ServiceRequestId`, `ProviderId`, `Price` `decimal(18,2)`, `Message` ≤1000, `EstimatedDate`, `Status` (`Pending`\|`Accepted`\|`Rejected`\|`Withdrawn`), `CreatedAt` |
| `Booking` | `Id`, `OfferId`, `ServiceRequestId`, `CustomerId`, `ProviderId`, `ScheduledDate`, `FinalPrice` `decimal(18,2)`, `Status` (`Scheduled`\|`InProgress`\|`Completed`\|`Cancelled`), `CreatedAt`, `StartedAt?`, `CompletedAt?`, `CancelledAt?`, `CancellationReason?`, `RowVersion` |
| `Review` | `Id`, `BookingId`, `CustomerId`, `ProviderId`, `Rating` 1–5, `Comment` ≤1000, `CreatedAt` |

### 5.3 Database constraints and indexes — the part that actually enforces the rules

Business rules in C# are advisory; a second browser tab is enough to get around them. These go
in `Data/Configurations/` and are the reason the workflow is trustworthy:

```csharp
// ── At most ONE accepted offer per request. This is what makes "accept exactly one" true.
builder.Entity<Offer>()
    .HasIndex(o => o.ServiceRequestId)
    .IsUnique()
    .HasFilter("[Status] = 'Accepted'")
    .HasDatabaseName("UX_Offer_OneAcceptedPerRequest");

// ── One offer per provider per request (no spam re-bidding).
builder.Entity<Offer>()
    .HasIndex(o => new { o.ServiceRequestId, o.ProviderId })
    .IsUnique()
    .HasFilter("[Status] <> 'Withdrawn'");

// ── Exactly one review per booking.
builder.Entity<Review>().HasIndex(r => r.BookingId).IsUnique();

// ── One booking per offer.
builder.Entity<Booking>().HasIndex(b => b.OfferId).IsUnique();

// ── A provider offers each service at most once.
builder.Entity<ProviderService>()
    .HasIndex(ps => new { ps.ProviderProfileId, ps.ServiceId }).IsUnique();

// ── Money: SQL Server silently truncates decimals with no precision configured.
builder.Entity<Offer>().Property(o => o.Price).HasPrecision(18, 2);
builder.Entity<Booking>().Property(b => b.FinalPrice).HasPrecision(18, 2);
builder.Entity<ServiceRequest>().Property(r => r.BudgetMin).HasPrecision(18, 2);
builder.Entity<ServiceRequest>().Property(r => r.BudgetMax).HasPrecision(18, 2);

// ── Enums as strings: readable in SSMS, and the filtered indexes above depend on it.
builder.Entity<Offer>().Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
builder.Entity<Booking>().Property(b => b.Status).HasConversion<string>().HasMaxLength(20);
builder.Entity<ServiceRequest>().Property(r => r.Status).HasConversion<string>().HasMaxLength(20);

// ── Rating bounds enforced by the DB, not just by DataAnnotations.
builder.Entity<Review>().ToTable(t =>
    t.HasCheckConstraint("CK_Review_Rating", "[Rating] BETWEEN 1 AND 5"));

// ── Optimistic concurrency → SQL Server rowversion.
builder.Entity<ServiceRequest>().Property(r => r.RowVersion).IsRowVersion();
builder.Entity<Booking>().Property(b => b.RowVersion).IsRowVersion();

// ── The provider request feed is the hottest query in the app. Index it.
builder.Entity<ServiceRequest>()
    .HasIndex(r => new { r.Status, r.ServiceId, r.City });

// ── Dashboard queries.
builder.Entity<ServiceRequest>().HasIndex(r => new { r.CustomerId, r.Status });
builder.Entity<Offer>().HasIndex(o => new { o.ProviderId, o.Status });
builder.Entity<Booking>().HasIndex(b => new { b.ProviderId, b.Status });
builder.Entity<Booking>().HasIndex(b => new { b.CustomerId, b.Status });
```

**Timestamps.** `DateTimeOffset` everywhere, always UTC on write
(`DateTimeOffset.UtcNow`), format for display in the browser only. Do not store local time.

**Cascades.** Default `Restrict` on `Booking → Offer` and `Review → Booking`; a completed job's
history must not be deletable. Nothing in the MVP hard-deletes a request that has offers —
cancelling sets a status.

---

## 6. State machines

These tables are the specification. Every transition is a service method that validates the
current state first and returns **409 Conflict** on an illegal move. This section is the direct
fix for v1's "cancel a request under allowed conditions", which never said what the conditions were.

### 6.1 ServiceRequest

| From | To | Trigger | Who | Guard |
|---|---|---|---|---|
| — | `Open` | create request | Customer | `PreferredDate` in the future |
| `Open` | `Booked` | an offer is accepted | Customer (owner) | set inside the accept transaction only |
| `Open` | `Cancelled` | cancel request | Customer (owner) | allowed **only** while `Open`; pending offers → `Rejected` |
| `Booked` | `Completed` | its booking completes | system | set by `BookingService` |
| `Booked` | `Cancelled` | its booking is cancelled | system | set by `BookingService` |
| `Completed` / `Cancelled` | — | terminal | — | no transitions out |

**Edit rule (v1 said "edit while still open"):** a request is editable only while `Open` **and it
has zero non-withdrawn offers**. Editing the price expectations under providers who already bid
is not acceptable. `Title`, `Description`, `PreferredDate`, `BudgetMin/Max` are editable;
`ServiceId` and `City` are not — changing those changes who was eligible to see it.

### 6.2 Offer

| From | To | Trigger | Who | Guard |
|---|---|---|---|---|
| — | `Pending` | submit offer | Provider | request `Open`; provider eligible (§7.4); no existing non-withdrawn offer by them |
| `Pending` | `Withdrawn` | withdraw | Provider (owner) | only while `Pending` |
| `Pending` | `Accepted` | customer accepts | Customer (request owner) | inside the transaction; at most one per request |
| `Pending` | `Rejected` | sibling accepted, or request cancelled | system | automatic, never a user action in v1 |
| `Accepted` / `Rejected` / `Withdrawn` | — | terminal | — | |

### 6.3 Booking

| From | To | Trigger | Who | Guard |
|---|---|---|---|---|
| — | `Scheduled` | offer accepted | system | created in the accept transaction |
| `Scheduled` | `InProgress` | start work | Provider (owner) | sets `StartedAt` |
| `Scheduled` | `Cancelled` | cancel | Customer **or** Provider (owner) | requires a reason; **only** before `InProgress` |
| `InProgress` | `Completed` | mark complete | Provider (owner) | sets `CompletedAt`; cascades request → `Completed` |
| `Completed` / `Cancelled` | — | terminal | — | a completed job cannot be reopened |

**Reviews:** creatable only when `Booking.Status == Completed`, only by `Booking.CustomerId`, and
only once (DB unique index). On create, recompute `ProviderProfile.AverageRating` and
`ReviewCount` in the same transaction.

> `AverageRating` is **stored, not computed on read** — the provider list would otherwise
> aggregate reviews for every row. Another entry for `docs/decisions.md`, with its cost: it must
> be recalculated in the same transaction as the review insert or it drifts.

---

## 7. API surface

REST-ish, `/api` prefix, `camelCase` JSON, `ProblemDetails` (RFC 9457) for every error.
`[ApiController]` gives `ValidationProblemDetails` on `ModelState` failure for free.

### 7.1 Auth & account

| Method | Route | Body / returns | Auth |
|---|---|---|---|
| `GET` | `/api/antiforgery/token` | sets the `XSRF-TOKEN` cookie | anon |
| `POST` | `/api/auth/register` | `{ email, password, fullName, role: "Customer"\|"Provider", city }` | anon |
| `POST` | `/api/auth/login` | `{ email, password }` → sets auth cookie, returns `MeDto` | anon |
| `POST` | `/api/auth/logout` | — | any |
| `GET` | `/api/auth/me` | `{ id, email, fullName, roles[], providerApproved? }` | any |

> `Admin` is **not** a self-registerable role — it is seeded. A public endpoint that accepts a
> role string and would mint an admin is the classic privilege-escalation bug; the endpoint must
> reject anything outside `Customer`/`Provider` server-side.

### 7.2 Catalog, profiles, workflow

| Method | Route | Purpose | Auth |
|---|---|---|---|
| `GET` | `/api/categories` | categories with their services | anon |
| `GET` | `/api/services?categoryId=` | services | anon |
| `POST` `PUT` `DELETE` | `/api/admin/categories[/{id}]` · `/api/admin/services[/{id}]` | catalog CRUD | Admin |
| `GET` `PUT` | `/api/providers/me` | own provider profile | Provider |
| `PUT` | `/api/providers/me/services` | `{ serviceIds: [] }` — replace the set | Provider |
| `GET` | `/api/providers/{id}` | public profile + rating + recent reviews | anon |
| `POST` | `/api/admin/providers/{id}/approval` | `{ isApproved }` | Admin |
| `GET` | `/api/service-requests/mine?status=&page=` | customer's own requests | Customer |
| `POST` | `/api/service-requests` | create | Customer |
| `GET` | `/api/service-requests/{id}` | detail — **response shape varies by caller** (§8.4) | Customer owner / eligible Provider / Admin |
| `PUT` | `/api/service-requests/{id}` | edit (guard §6.1) | Customer owner |
| `POST` | `/api/service-requests/{id}/cancel` | cancel | Customer owner |
| `GET` | `/api/service-requests/available?page=` | **the eligibility feed** (§7.4) | Provider |
| `GET` | `/api/service-requests/{id}/offers` | offers on a request | Customer owner |
| `POST` | `/api/service-requests/{id}/offers` | submit an offer | Provider |
| `GET` | `/api/offers/mine?status=` | provider's own offers | Provider |
| `POST` | `/api/offers/{id}/withdraw` | withdraw | Provider owner |
| `POST` | `/api/offers/{id}/accept` | **the transaction** → returns the new `Booking` | Customer owner |
| `GET` | `/api/bookings/mine?role=&status=` | bookings for the caller | Customer / Provider |
| `GET` | `/api/bookings/{id}` | detail | participants + Admin |
| `POST` | `/api/bookings/{id}/start` · `/complete` · `/cancel` | transitions (§6.3) | per the table |
| `POST` | `/api/bookings/{id}/review` | create the one review | Customer owner |
| `GET` | `/api/admin/stats` | 4 counters | Admin |

### 7.3 Status code contract

| Situation | Status |
|---|---|
| Field validation failed | `400` + `ValidationProblemDetails` |
| Not logged in | `401` — **never a 302 redirect** (§8.2) |
| Logged in, wrong role, or not your row | `403` |
| Row absent, or hidden from this caller | `404` |
| Legal request, illegal state (`accept` on a `Booked` request, `complete` on a `Scheduled` booking) | `409` |
| Lost the concurrency race | `409` with a "someone else accepted first" message |

### 7.4 The eligibility rule — defined (v1 left this vague)

v1 said *"only eligible providers should be able to view relevant requests"* and never defined
eligible. That single sentence is the most security-sensitive rule in the app. **Definition:**

> Provider `P` may see `ServiceRequest R` **iff all four hold**:
> 1. `R.Status == Open`
> 2. `P.IsApproved == true`
> 3. a `ProviderService` row exists for `(P, R.ServiceId)` — they offer that service
> 4. `P.City == R.City` — exact, case-insensitive match

Enforced **once**, in `ServiceRequestService`, and used by both `GET /available` (the list) and
`GET /{id}` (the detail). One method, two callers — if the list filters but the detail doesn't,
every hidden request is one URL guess away.

City matching by exact string is a v1 simplification. Record the limitation and the successor
(a service-area radius, V2) in `docs/decisions.md`.

**Field-level rule:** an eligible provider sees the request but **not the customer's name, phone,
email, or street address** until their offer is accepted. Two DTOs from one entity:
`RequestSummaryForProviderDto` and `RequestDetailForCustomerDto`. This is why §4 forbids
returning entities from controllers.

---

## 8. Authentication, authorization, security

### 8.1 Why cookies and not a JWT in localStorage

The default instinct for "React + API" is a bearer token in `localStorage`. For this project
that's the wrong call, and knowing why is part of the learning:

| | Cookie session (chosen) | JWT in `localStorage` |
|---|---|---|
| XSS exposure | `HttpOnly` — JS cannot read it | Any injected script exfiltrates the token |
| CSRF exposure | Real — mitigated with anti-forgery tokens (§8.3) | None |
| Logout / revocation | Server-side, immediate | Token stays valid until expiry unless you build a denylist |
| Refresh flow | None needed | Refresh tokens, rotation, races — days of work |
| Fit with Identity | `SignInManager` + `[Authorize]` work as-is | Extra plumbing |

Client and API are same-origin in production (React is served from the API's `wwwroot`) and
same-origin in dev via the Vite proxy, so cookies need no CORS relaxation. Cookie config:
`HttpOnly = true`, `SameSite = Lax`, `SecurePolicy = Always` in production, sliding expiration.

### 8.2 The SPA gotcha that costs people an afternoon

Identity's cookie handler **redirects unauthenticated requests to `/Account/Login`** — a `302`
your `fetch` follows, handing back an HTML login page with status `200`. The client then tries to
`JSON.parse` HTML. Override it:

```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;   // relax only for local http
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;

    options.Events.OnRedirectToLogin = ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});
```

### 8.3 CSRF — copy this, don't derive it

Cookie auth needs anti-forgery. Double-submit cookie pattern; this is the one piece of plumbing
to take as given:

```csharp
builder.Services.AddAntiforgery(o => o.HeaderName = "X-XSRF-TOKEN");
builder.Services.AddControllers(o =>
    o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));  // guards all non-GET

// AntiforgeryController
[HttpGet("/api/antiforgery/token")]
public IActionResult GetToken([FromServices] IAntiforgery antiforgery)
{
    var tokens = antiforgery.GetAndStoreTokens(HttpContext);
    Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!,
        new CookieOptions { HttpOnly = false, SameSite = SameSiteMode.Lax, Secure = true });
    return NoContent();
}
```

React calls that endpoint once on boot, then the fetch wrapper reads the readable `XSRF-TOKEN`
cookie and sends it as `X-XSRF-TOKEN` on every mutation. `login` and `register` need
`[IgnoreAntiforgeryToken]` **or** the client must fetch the token before its first POST — pick
the second, it's the safer habit.

### 8.4 Authorization matrix

Role checks (`[Authorize(Roles = "Provider")]`) are necessary and **not sufficient** — they say
*a* provider may call this, not that *this* provider owns *this* row. Every row-scoped operation
re-checks ownership in the service layer.

| Operation | Role gate | Row-level check |
|---|---|---|
| Create request | Customer | — |
| View request detail | Customer / Provider / Admin | owner, **or** passes all four eligibility tests (§7.4), **or** Admin |
| Edit / cancel request | Customer | `request.CustomerId == me` |
| List available requests | Provider | eligibility filter applied in the query |
| Submit offer | Provider | eligible + approved + no duplicate offer |
| View offers on a request | Customer | `request.CustomerId == me` |
| Withdraw offer | Provider | `offer.ProviderId == me` |
| Accept offer | Customer | `offer.Request.CustomerId == me` |
| Start / complete booking | Provider | `booking.ProviderId == me` |
| Cancel booking | Customer or Provider | participant, and `Status == Scheduled` |
| Create review | Customer | `booking.CustomerId == me` + `Completed` + none exists |
| Admin endpoints | Admin | — |

**The test that must pass:** log in as Provider B, request Provider A's offer id, get `403` or
`404`. Not a 200. This IDOR check is the single most common defect in an app of this shape, and
it is on the rubric.

### 8.5 Secrets

`appsettings.json` ships **no** connection string, no admin seed password. Locally:

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Default" "Server=(localdb)\\MSSQLLocalDB;Database=Khidma;Trusted_Connection=True;MultipleActiveResultSets=true"
dotnet user-secrets set "Seed:AdminPassword" "<dev only>"
```

Deployed: Azure App Service configuration. A committed secret in the Git history costs rubric
points even if a later commit removes it — history is forever, and `git log -p` is where a
reviewer looks first.

---

## 9. The 4-week plan

Format per week: goal → tasks → **definition of done** → mentor checkpoint. A week is not
complete until its DoD demos live on `develop`. Falling behind is expected — use §14's cut
list, don't silently drop DoD items.

**The rule that makes 4 weeks possible:** something runs end to end from **day 4**. v1 spent
three weeks before the first working page and left all integration risk for the end. Here, week 1
ends with a real login against a real database rendered by real React, and the following three
weeks only add features to a working system.

---

### Week 1 — Skeleton + vertical slice + auth

**Goal:** register, log in, see the seeded catalog, refresh the page and still be logged in.

**Day 1 — repo and tooling**
- [ ] GitHub repo. `.gitignore` (dotnet + node). `main` + `develop`; protect `main` — no direct
      pushes, PR required.
- [ ] `README.md` skeleton: what it is, prerequisites, how to run.
- [ ] `dotnet new webapi -n Khidma.Api`, solution, `Khidma.Api.Tests` project wired in.
- [ ] `npm create vite@latest client -- --template react-ts`; both apps start.
- [ ] `.github/workflows/ci.yml`: `dotnet build` + `dotnet test` + `npm ci && npm run build`.
      **Get CI green today** — a red pipeline you plan to fix later stays red.

**Day 2 — data layer**
- [ ] All nine entities + enums under `Domain/`.
- [ ] `AppDbContext`, one `IEntityTypeConfiguration<T>` per entity, every constraint from §5.3.
- [ ] Identity wired: `ApplicationUser`, `IdentityRole`, connection string in user-secrets.
- [ ] First migration; `dotnet ef database update`; **inspect the schema in SSMS** and confirm the
      filtered indexes, `decimal(18,2)`, and `rowversion` columns actually exist. Configuration
      that silently didn't apply is the week-3 bug you can't find.
- [ ] `DbSeeder`: 3 roles, 1 admin, ~4 categories, ~12 services, 2 customers, 3 providers with
      services and cities. Idempotent — safe to run on every startup.

**Day 3 — auth API**
- [ ] Cookie configuration + the 401/403 override (§8.2).
- [ ] `AuthController`: register (role whitelisted to Customer/Provider — reject `Admin`), login,
      logout, `me`. Register creates the matching profile row in the same transaction.
- [ ] Anti-forgery setup + token endpoint (§8.3).
- [ ] Global exception handler → `ProblemDetails`; `CatalogController` read endpoints.
- [ ] Swagger/OpenAPI on in Development. Exercise every endpoint there before touching React.

**Day 4 — React shell, wired for real**
- [ ] `vite.config.ts`: `server.proxy['/api'] → https://localhost:5001`,
      `build.outDir = '../server/Khidma.Api/wwwroot'`, `emptyOutDir: true`.
- [ ] `api/client.ts`: one wrapper — `credentials: 'include'`, injects `X-XSRF-TOKEN`, throws a
      typed `ApiError` carrying status + `ProblemDetails`. Every call goes through it.
- [ ] `AuthContext`: on mount, call `/api/auth/me` to rehydrate. The cookie is `HttpOnly`, so the
      client **cannot** read its own session — it has to ask the server who it is. Render nothing
      role-dependent until that resolves, or the UI flashes the wrong shell.
- [ ] `RequireAuth` / `RequireRole` route wrappers; React Router routes; nav that reflects state.
- [ ] `styles/tokens.css` (colors, spacing, radius, type scale) + `global.css` reset.

**Day 5 — slice closed, then integrate**
- [ ] Register, Login, Home, Catalog browse pages. Real server validation errors rendered under
      the right fields.
- [ ] Three empty role dashboards behind role guards.
- [ ] Production check: `npm run build`, then run the API alone with
      `app.MapFallbackToFile("index.html")` — the whole app must work on one origin, one port.
      Discovering this on day 25 is a bad day.
- [ ] Open the PR into `develop`. Mentor reviews.

**DoD:** All three roles register/log in against SQL Server. Session survives F5. Catalog renders
from the API. `/api/auth/me` returns `401` (not HTML) when logged out. CI green. Production build
serves from a single origin.

**Checkpoint:** review the schema in SSMS and the `api/client.ts` wrapper. If the constraints and
the fetch wrapper aren't right now, everything after inherits it.

---

### Week 2 — Requests, offers, and the service layer

**Goal:** the request→offer half of the workflow, with the eligibility rule and the first tests.

**Day 1–2 — requests**
- [ ] `IServiceRequestService` + implementation: create, get-mine, get-by-id (caller-aware),
      edit (guard: `Open` + zero non-withdrawn offers), cancel (§6.1).
- [ ] DTOs in / DTOs out. **No entity crosses the controller boundary.** Separate
      `RequestDetailForCustomerDto` and `RequestSummaryForProviderDto` (§7.4).
- [ ] Validation: `PreferredDate` in the future; `BudgetMax >= BudgetMin`; lengths. Both as
      DataAnnotations **and** re-checked in the service — the service is what tests call.
- [ ] Pagination on every list from the start. `?page=&pageSize=` with a server-side max of 50.
      Retrofitting pagination after the UI assumes an array is annoying.
- [ ] React: create-request form, "my requests" list with status badges, request detail.

**Day 3 — eligibility**
- [ ] Implement §7.4 as **one** method used by both the list and the detail endpoints.
- [ ] `GET /available` for providers, paginated, `Include`-projected — no N+1. Check the generated
      SQL in the EF Core logs at least once and be able to explain it.
- [ ] Provider profile page: city, experience, bio, service multi-select
      (`PUT /providers/me/services`).
- [ ] React: provider "available requests" feed + detail, with contact fields absent.

**Day 4 — offers**
- [ ] `IOfferService`: submit (eligible + approved + request `Open` + no duplicate), withdraw
      (owner + `Pending`), list-mine.
- [ ] React: offer form on the request detail, provider "my offers" list, customer's offers-on-my-
      request list.

**Day 5 — tests, and this is not optional**
- [ ] xUnit + SQLite in-memory harness (§11).
- [ ] **Minimum six service tests:** ineligible provider can't see a request · unapproved
      provider can't offer · duplicate offer rejected · offer on a non-`Open` request rejected ·
      non-owner can't edit a request · request with offers can't be edited.
- [ ] **Two integration tests** via `WebApplicationFactory`: anonymous `POST /service-requests`
      → 401; Provider B reading Provider A's offer → 403/404.
- [ ] PR into `develop`. Mentor review.

**DoD:** A customer creates a request; only genuinely eligible providers see it; a provider offers
once and can withdraw; the customer sees the offers. 8+ tests green in CI. No business `if` in any
controller.

**Checkpoint:** read the service layer with the mentor. The question to answer: *"where is this
rule enforced, and what happens if I call the API directly with Postman?"*

---

### Week 3 — Accept, book, complete, review

**Goal:** the workflow closes. This is the highest-value week — protect it, cut elsewhere.

**Day 1–2 — the accept-offer transaction (the centrepiece)**

v1 asked for "customer can accept exactly one offer" with no mechanism. Two browser tabs break a
naive implementation. Correct version:

```csharp
public async Task<Result<BookingDto>> AcceptOfferAsync(int offerId, string userId, CancellationToken ct)
{
    await using var tx = await _db.Database.BeginTransactionAsync(ct);

    var offer = await _db.Offers
        .Include(o => o.ServiceRequest)
        .SingleOrDefaultAsync(o => o.Id == offerId, ct);

    if (offer is null)                                    return Result.NotFound();
    if (offer.ServiceRequest.CustomerId != userId)         return Result.Forbidden();
    if (offer.Status != OfferStatus.Pending)               return Result.Conflict("Offer is no longer pending.");
    if (offer.ServiceRequest.Status != ServiceRequestStatus.Open)
                                                           return Result.Conflict("Request is no longer open.");

    offer.Status = OfferStatus.Accepted;

    // Siblings rejected in one round trip — no load-then-loop.
    await _db.Offers
        .Where(o => o.ServiceRequestId == offer.ServiceRequestId
                 && o.Id != offer.Id
                 && o.Status == OfferStatus.Pending)
        .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, OfferStatus.Rejected), ct);

    offer.ServiceRequest.Status = ServiceRequestStatus.Booked;   // RowVersion checked on save

    var booking = new Booking
    {
        OfferId          = offer.Id,
        ServiceRequestId = offer.ServiceRequestId,
        CustomerId       = offer.ServiceRequest.CustomerId,
        ProviderId       = offer.ProviderId,
        ScheduledDate    = offer.EstimatedDate,
        FinalPrice       = offer.Price,
        Status           = BookingStatus.Scheduled,
        CreatedAt        = DateTimeOffset.UtcNow
    };
    _db.Bookings.Add(booking);

    try
    {
        await _db.SaveChangesAsync(ct);          // RowVersion mismatch → DbUpdateConcurrencyException
        await tx.CommitAsync(ct);
    }
    catch (DbUpdateConcurrencyException)
    {
        await tx.RollbackAsync(ct);
        return Result.Conflict("Another action changed this request. Please reload.");
    }
    catch (DbUpdateException ex) when (IsUniqueViolation(ex))   // UX_Offer_OneAcceptedPerRequest
    {
        await tx.RollbackAsync(ct);
        return Result.Conflict("Another offer was accepted first.");
    }

    return Result.Success(booking.ToDto());
}
```

- [ ] Implement it. **Three defences, deliberately:** the status guard (fast, friendly), the
      `RowVersion` check (catches the interleaving), the filtered unique index (the guarantee that
      holds even if the C# is wrong). Be able to explain what each one catches that the others
      don't — that's the interview answer this project buys.
- [ ] Prove the race by hand: two tabs, both on the offers page, accept in both. The second gets
      a clean `409` and a reload prompt, not a 500 and not two bookings.

**Day 3 — booking lifecycle**
- [ ] `IBookingService`: start, complete, cancel — the §6.3 table exactly. Illegal transition →
      `409`. Set `StartedAt` / `CompletedAt` / `CancelledAt` + reason.
- [ ] Completing a booking cascades the request to `Completed`; cancelling cascades to `Cancelled`.
- [ ] React: booking detail with the status timeline; provider action buttons that are disabled by
      state **and** re-validated server-side.

**Day 4 — reviews and dashboards**
- [ ] `IReviewService`: create — `Completed` + owner + none exists; recompute
      `ProviderProfile.AverageRating` and `ReviewCount` **in the same transaction**.
- [ ] Public provider profile: rating, review count, recent reviews.
- [ ] Fill in the three dashboards. Customer: open requests, offers awaiting a decision, active
      bookings, awaiting-review. Provider: eligible requests, pending offers, active jobs, rating.
      Admin: catalog CRUD, provider approval toggle, 4 counters.

**Day 5 — tests for the hard parts**
- [ ] Accept-offer: happy path creates exactly one booking · siblings all `Rejected` · accepting a
      second offer → `409` · non-owner → `403`.
- [ ] Booking: every legal transition passes; `Scheduled → Completed`, `Completed → InProgress`,
      and cancel-after-start all rejected.
- [ ] Review: on an incomplete booking → rejected · second review → rejected · average
      recomputed correctly across 3 reviews.
- [ ] PR into `develop`. Mentor review.

**DoD:** The full workflow runs end to end in the browser with zero manual DB edits. Concurrent
accepts produce one booking. Illegal transitions return 409. 20+ tests green.

**Checkpoint:** the mentor tries to break it — accept twice, review twice, complete a scheduled
booking, curl another user's booking. Every attempt should fail cleanly.

---

### Week 4 — Hardening, quality, deploy, demo

**Goal:** turn a working app into a defensible one.

**Day 1 — security and correctness pass**
- [ ] **Walk the §8.4 matrix row by row** with two logged-in browser profiles, using Swagger or
      curl for the calls the UI won't make. Every cross-user attempt: 403 or 404. Log the results
      as a table in `docs/`.
- [ ] Confirm anti-forgery rejects a mutation with no `X-XSRF-TOKEN` header (403), and that the
      client always sends it.
- [ ] Confirm no endpoint returns another user's email/phone before acceptance.
- [ ] Confirm registration cannot mint an `Admin`.
- [ ] `git log -p | grep -i -E 'password|connectionstring|secret'` — history is forever.

**Day 2 — data-access and API quality**
- [ ] Turn on EF Core sensitive-data logging in Development and read the SQL for the three
      dashboards and the available-requests feed. Fix N+1s with projection; fix missing indexes.
      Note before/after query counts in `docs/decisions.md`.
- [ ] Confirm pagination on every list; confirm the server caps `pageSize`.
- [ ] Every endpoint returns `ProblemDetails` on failure. No stack trace ever reaches the client
      in Production.
- [ ] Structured logging on the state-changing operations (accept, transitions, review) with the
      actor id. "It broke and there's no log" is not a debuggable position.

**Day 3 — frontend quality**
- [ ] Every async view has all four states: loading, empty, error, success. Skipping "empty" and
      "error" is the most common thing missing from student React.
- [ ] Forms: disabled while submitting (no double-submit), server field errors mapped to fields,
      success feedback.
- [ ] Responsive at 360 px, 768 px, 1280 px. Real CSS Grid/Flexbox, not fixed pixel widths.
- [ ] Accessibility basics: labels tied to inputs, keyboard-reachable actions, visible focus,
      status conveyed by text and not colour alone. Run Lighthouse once; fix what's cheap.
- [ ] 5 Vitest/RTL tests: login form validation, `RequireRole` redirect, request list empty state,
      offer form submit, error banner on `ApiError`.

**Day 4 — deploy**
- [ ] `dotnet ef migrations script --idempotent -o migrate.sql` — deploy by running the script,
      not `Database.Migrate()` at startup. Explain why in the README (concurrent instances,
      irreversible auto-migration, no review step).
- [ ] Azure App Service (Windows) + Azure SQL. Connection string in App Settings. HTTPS only.
      `ASPNETCORE_ENVIRONMENT=Production`.
- [ ] Deploy the single artifact (API + built React in `wwwroot`), run the migration script, seed
      the catalog and an admin.
- [ ] Smoke-test the whole workflow on the deployed URL. **The deployed environment always breaks
      something local didn't** — cookie `Secure`, HTTPS redirect, SPA fallback on deep links.
      Budget for it.

**Day 5 — documentation and demo**
- [ ] `README.md`: what it is · screenshots · stack · **prerequisites and exact run steps a
      stranger can follow** · architecture diagram · data model diagram · how to run tests ·
      deployed URL · demo credentials for all three roles · known limitations.
- [ ] `docs/decisions.md`: 6–10 short ADRs. Cookies over JWT · denormalized ids on Booking ·
      stored `AverageRating` · exact-city eligibility · three-layer accept defence · idempotent
      migration script.
- [ ] `docs/demo-script.md` (§15) and **rehearse it twice on the deployed URL**.
- [ ] Final PR `develop → main`. Tag `v1.0.0`.
- [ ] Present.

**DoD:** Deployed and reachable. README lets a stranger run it. Authorization matrix verified and
recorded. 25+ backend tests and ~5 frontend tests green in CI. Demo rehearsed.

---

## 10. Definition of done — per feature, not just per week

A feature is done when **all seven** hold. This is the checklist for the intern's own PR
self-review:

1. Rule enforced in `Services/`, not in a controller or a component.
2. Ownership/authorization re-checked server-side, independent of what the UI shows.
3. Correct status code for each failure mode (§7.3).
4. Input validated server-side; the client renders the server's messages.
5. At least one test covering the rule — and one covering its rejection path.
6. UI has loading, empty, error, and success states.
7. Merged via a reviewed PR with a message that says why, not just what.

---

## 11. Testing strategy

| Layer | Tool | Count | Covers |
|---|---|---|---|
| Service unit tests | xUnit + SQLite in-memory | ~20 | Eligibility, offer rules, transitions, review rules, ownership |
| API integration | xUnit + `WebApplicationFactory<Program>` | ~6 | Auth required, role gates, IDOR, anti-forgery, one happy path per role |
| Concurrency | Manual, two browser tabs, documented | 1 | Double-accept produces one booking |
| Frontend | Vitest + RTL | ~5 | Route guards, form validation, error rendering |
| Manual scenarios | The v1 §13 table, kept | 8 | Recorded pass/fail in `docs/` |

**Use SQLite in-memory, not EF Core InMemory.** `Microsoft.EntityFrameworkCore.InMemory` is not a
relational provider: it ignores unique indexes, check constraints, and `rowversion`. A test suite
on it goes green while the real database rejects the same write — worse than no tests, because it
manufactures false confidence.

```csharp
public abstract class DbTestBase : IAsyncLifetime
{
    private SqliteConnection _conn = default!;
    protected AppDbContext Db = default!;

    public async Task InitializeAsync()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        await _conn.OpenAsync();                       // schema lives as long as the connection
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_conn).Options;
        Db = new AppDbContext(options);
        await Db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await Db.DisposeAsync();
        await _conn.DisposeAsync();
    }
}
```

**Known limitation, and say so in the README:** SQLite has no filtered indexes and no
`rowversion`, so `UX_Offer_OneAcceptedPerRequest` and the concurrency token are **not** exercised
by the unit suite. Those two are verified manually against SQL Server (week 3 day 2) and the
result is recorded. Knowing the gap in your own test coverage and stating it is worth more on the
rubric than pretending it isn't there.

---

## 12. Git, CI, and review discipline

This is the habit that outlasts the framework. v1 taught `git push`; do this instead.

**Branches.** `main` (protected, deployable, tagged releases) · `develop` (integration) ·
`feature/<short-name>` (one feature, short-lived).

**Every change reaches `develop` through a PR.** No direct pushes, including your own. Protect
`main` and `develop` in GitHub settings: require a passing CI check, require one review.

**Commits.** Imperative mood, why over what, one logical change each.

```
feat(offers): reject duplicate offers from the same provider

A provider could bid twice on one request and flood the customer's list.
Guarded in OfferService and backed by a filtered unique index on
(ServiceRequestId, ProviderId) so a concurrent double-POST can't slip through.
```

Bad: `update` · `fix` · `work` · `final` · `asdf` · `week 3 stuff`.

**PR description template:** what changed · why · how to test it · screenshots for UI · what you're
unsure about. That last line is what makes a review useful instead of ceremonial.

**CI (`.github/workflows/ci.yml`) on every PR:** setup .NET 10 → restore → build with warnings as
errors → `dotnet test`; setup Node 22 → `npm ci` → `tsc --noEmit` → `npm run build` → `vitest run`.
Red CI blocks merge. Keep it green from day 1 — a pipeline that's been red for a week is a
pipeline nobody reads.

---

## 13. Grading rubric (100 points)

| # | Criterion | Pts | What full marks looks like |
|---|---|---|---|
| 1 | **Workflow correctness** | 25 | All 11 MVP items work end to end with no manual DB edits. Accept-offer is transactional and concurrency-safe. Every state machine in §6 enforced; illegal transitions → 409. |
| 2 | **Data model & EF Core** | 15 | All §5.3 constraints present in the real schema. Correct cardinalities and navigations. Clean incremental migrations (not one squashed "init" at the end). Money precision, UTC timestamps, string enums. |
| 3 | **Security & authorization** | 15 | §8.4 matrix holds under direct API calls. No IDOR. No privilege escalation at registration. Anti-forgery working. Contact details hidden pre-acceptance. No secrets in Git history. |
| 4 | **Backend code quality** | 12 | Controllers thin, rules in `Services/`, DTOs at the boundary, consistent `ProblemDetails`, no N+1 in the dashboards, dependency injection used properly. |
| 5 | **Frontend quality** | 13 | Sensible component structure, one typed API client, auth context correct across refresh, all four async states everywhere, responsive, basic a11y, TypeScript not `any`-ridden. |
| 6 | **Testing** | 10 | 25+ meaningful backend tests including rejection paths, ~5 frontend tests, CI green, coverage gaps stated honestly. |
| 7 | **Git & process** | 10 | Steady commit history across all 4 weeks (not 3 commits in week 4), meaningful messages, PR-per-feature, protected branches, CI from day 1. |

**Bonus, up to +5:** the deployed app is live and stable · `docs/decisions.md` shows real
engineering reasoning · demonstrably measured and fixed a performance problem · found and fixed a
security hole in their own code and wrote it up.

**Automatic deductions:** business logic in controllers (−5) · secret committed at any point (−5) ·
tests that don't actually assert (−5) · `catch { }` swallowing errors (−3) · a rule enforced only
by hiding a button (−5).

---

## 14. If you fall behind — cut in this order

Do not silently drop a DoD item. Cut top-down, and record what you cut and why in the README.
Judgement about scope under pressure is itself a graded skill.

1. Frontend tests (§ week 4 day 3) — down to zero.
2. Admin catalog CRUD **UI** — keep the API, manage via Swagger, seed the catalog.
3. Provider approval — auto-approve on registration, keep the column and the eligibility check.
4. Pagination UI — keep server-side paging, show page 1 only.
5. `BudgetMin`/`BudgetMax` — drop from the form and the DTO, keep the columns.
6. Booking cancellation — reduce to the happy path only, document the omission.
7. Deployment — demo locally, but the README's deploy steps must be complete and honest.

**Never cut these — they are 55 rubric points:**

- The accept-offer transaction and its concurrency safety.
- Object-level authorization (§8.4).
- The one-review-per-completed-booking rule.
- The ~12 service tests covering the rules above.
- A README a stranger can follow.

---

## 15. Final demo script (10 minutes, rehearsed twice)

One continuous story. Never open SSMS to make something work.

1. **(0:30)** One slide: the problem, the workflow, the stack. Show the architecture diagram.
2. **(1:00)** Log in as **Admin** → catalog, approve a pending provider, the stat strip.
3. **(1:30)** Log in as **Customer** → create a request: "AC not cooling", Riyadh, next Tuesday,
    budget 200–400. Show the validation rejecting a past date first.
4. **(1:30)** Log in as **Provider A** → the request appears in the feed. Point out: *no customer
    phone number on this screen, by design.* Send an offer at 300.
5. **(0:45)** **Provider B** — an *ineligible* provider, wrong city → the same request is absent.
    Paste its URL directly → 403. **This is the slide that separates a working app from a correct
    one.** Don't rush it.
6. **(1:30)** Back to **Customer** → two offers listed → accept A's. Booking appears. Show B's
    offer now `Rejected` automatically.
7. **(1:00)** **The race.** Two tabs on the offers page, accept in both. Second tab: a clean
    "another offer was accepted first" 409. Then show the three defences in the code.
8. **(1:00)** **Provider A** → start → complete. Show the illegal transition rejected too.
9. **(1:00)** **Customer** → review, 5 stars → provider's public rating updates.
10. **(0:45)** `dotnet test` running green; the CI badge; the commit graph across 4 weeks.
11. **(0:30)** What you'd build next and what you'd do differently. Have a real answer — the honest
    version scores better than a polished non-answer.

**Expect these questions.** Prepare them:

- Why cookies instead of a JWT?
- What exactly stops two customers accepting two offers on one request?
- Show me where "only eligible providers see this request" is enforced. Once, or twice?
- Why is `AverageRating` a stored column?
- What's the biggest weakness in this codebase? *(Not "nothing." Name one and know its fix.)*

---

## 16. Backlog beyond v1 (unchanged intent from v1's V2–V4)

**V2 — professional polish.** Photo uploads on requests (with real content-type and size
validation) · search and filters · service-area radius instead of exact city match · in-app
notifications · favourites · provider portfolio · cancellation reasons and complaints · admin
analytics.

**V3 — advanced.** Map/location integration · provider availability calendar · real-time
notifications (SignalR) · smarter request↔provider matching · certificate/document upload and
verification.

**V4 — business.** Platform commission · provider subscription tiers · promoted listings ·
payment integration when legally and technically appropriate · public API and a mobile client.

**Only after v1 is stable:** Docker for reproducible local setup · Redis caching on the catalog ·
extracting a read model for dashboards · TanStack Query · CQRS-ish separation. Every one of these
is a good idea and every one is a bad idea in week 3.

---

*End of plan v2 — React SPA + ASP.NET Core Web API, 4 weeks.*
