# Demo script (plan v2 §15)

Ten minutes, one continuous story. Never open SSMS to make something work. Passwords are `Seed:AdminPassword` / `Seed:DemoPassword` in user secrets.

Demo service is **Plumbing in Ramallah**. There is no seeded “AC not cooling” service. Cities stay the Palestinian seeder (not Riyadh). Ineligible direct URLs are **404**, not 403.

| Role in the script | Email | Notes |
| --- | --- | --- |
| Admin | `admin@khidma.local` | Catalog, verification, stats |
| Customer | `customer@khidma.local` | Ramallah |
| Provider A | `provider1@khidma.local` | Ramallah, Approved, Home Services |
| Provider B (ineligible) | `provider3@khidma.local` | Bethlehem, Approved — must **not** see Ramallah Plumbing |
| Provider C (second offer) | `provider4@khidma.local` | Ramallah, Approved, Plumbing |

## 11 steps

1. **(0:30)** One slide: the problem (local booking without a phone tree), the workflow (request → offer → one accept → booking → review), the stack (React SPA + ASP.NET Core + SQL Server, cookie auth). Show the README architecture diagram.

2. **(1:00)** Log in as **Admin**. Catalog is seeded (Home Services / Plumbing). Open **Provider Verification**: `provider2` is `PendingReview`. Overview shows the stat strip and needs-attention queue.

3. **(1:30)** Log in as **Customer**. Create a request: Plumbing, Ramallah, a future preferred date, budget 200–400, title “Kitchen sink leaking under the cabinet”, description “Water pooling under the sink since yesterday. Need someone to check the pipe and seal.” First submit a **past date** and show the validation error, then use a future date.

4. **(1:30)** Log in as **Provider A** (`provider1`). The request is on **Available requests**. Point out: *no customer email or phone on this screen, by design.* Submit an offer (price 300, future estimated date).

5. **(0:45)** Log in as **Provider B** (`provider3`, Bethlehem). The Ramallah Plumbing request is **absent** from the feed. Paste the request URL directly → **404** (never 200). This is eligibility, not a UI filter.

6. **(1:30)** Log in as **Provider C** (`provider4`). Submit a second offer. Back as the **Customer**, two offers are listed. Accept Provider A’s. Booking appears. Provider C’s offer is **Rejected**.

7. **(1:00)** **The race.** Two browser tabs on the customer request (or A vs C before step 6 is finished). Accept in both. The loser shows **Another offer was accepted first.** plus **Reload offers**. Three defences: state, `rowversion`, filtered unique index `UX_Offer_OneAcceptedPerRequest`.

8. **(1:00)** **Provider A** opens the booking → **Start work** → **Complete job**. Try complete while still `Scheduled` → **409**.

9. **(1:00)** **Customer** leaves a 5-star review. The public provider page rating updates. A second review → **409**.

10. **(0:45)** `dotnet test -c Release` and `npm run test` green. CI jobs **Backend** and **Frontend**. Four-week commit graph on `feature/full-project-development`.

11. **(0:30)** Next: Blob Storage for documents, Azure App Service from `deploy/AZURE_DEPLOY.md`. LocalDB NVMe recovery is documented (README / ADR 22). Keep CSRF as the framework 400.

## Expected questions

- Why cookies instead of JWT? Same-origin SPA; HttpOnly cookie + antiforgery. ADR 1.
- What stops two accepts on one request? State + rowversion + filtered unique index. ADR 5.
- Where is eligibility enforced? Once: `EligibleOpenRequestsForProvider`, reused by list and detail. ADR 2 / 14.
- Why is `AverageRating` stored? List/public cards; recomputed in the review transaction. ADR 4.
- Biggest weakness? Local disk documents (not multi-instance). LocalDB on NVMe needed the sector workaround (README / ADR 22) before live SQL proofs. Fixes: Blob Storage; registry + Restart if LocalDB will not start.
