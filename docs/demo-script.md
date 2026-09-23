# Demo script

Ten minutes, one continuous story. Never open SSMS to make something work. Passwords are `Seed:AdminPassword` / `Seed:DemoPassword` in user secrets.

Demo service is **Plumbing in Ramallah**. Cities stay the Palestinian seeder. A provider who does not offer the service, or who is not approved, cannot be booked.

| Role in the script | Email | Notes |
| --- | --- | --- |
| Admin | `admin@khidma.local` | Catalog, verification, stats. Phone `+970 0590000001` |
| Customer | `customer@khidma.local` | Ramallah. Phone `+970 0591111111` |
| Provider A | `provider1@khidma.local` | Ramallah, Approved, Plumbing. Phone `+970 0593333333` |
| Provider B | `provider3@khidma.local` | Bethlehem, Approved, cleaning and tutoring — not listed for Ramallah Plumbing |
| Provider C | `provider4@khidma.local` | Ramallah, Approved, Plumbing. Phone `+970 0596666666` |

Seeded bookings for the demo customer: a **Pending** plumbing request with Provider A, a **Scheduled** plumbing visit with Provider C (quoted 180), and a **Completed** electrical job with Provider A plus a 5-star review.

## Steps

1. **(0:30)** One slide: the problem (booking a local professional without a phone tree), the workflow (catalog → provider → pick a day → provider accepts with a price, start, and duration → start → complete → review), the stack (React SPA + ASP.NET Core + SQL Server, cookie auth).

2. **(1:00)** Log in as **Admin**. Catalog is seeded (Home Services / Plumbing). Open **Provider Verification**: `provider2` is `PendingReview`. Overview funnel is requested / accepted / completed. Attention includes stale pending bookings.

3. **(1:30)** Log out. Open **Catalog** as a guest and click Plumbing. The city filter can be set to Ramallah. Provider A and Provider C appear. Provider B does not. Click **Book** while signed out and confirm the login page keeps the return path.

4. **(1:30)** Register a new customer with a phone number, or log in as **Customer**. From Plumbing, book Provider A and pick an open day on the calendar. Closed weekdays stay disabled. The booking detail shows both phone numbers. The public provider page still does not. It does show the weekly hours summary.

5. **(1:30)** Log in as **Provider A**. Open **Schedule** and show Sun–Thu working hours, then the pending request on the dashboard. Open it, call out the customer phone, then **Accept** with a price, a start time, and a duration. Status becomes Scheduled and the week grid fills those hours. **Reschedule** can move the visit or change the duration; the customer sees the note. Decline is the path for a pending request the provider will not take. Cancelling a pending request is the customer's action.

6. **(1:00)** **Provider A** starts the visit, then completes it. Completing while still Scheduled returns **409**.

7. **(1:00)** **Customer** opens **My Bookings**, leaves a 5-star review on the completed job, and opens **Profile** to show phone, city, and password. A second review returns **409**.

8. **(0:45)** `dotnet test -c Release` and `npm test` green. CI jobs **Backend** and **Frontend**.

## Expected questions

- Why cookies instead of JWT? Same-origin SPA; HttpOnly cookie + antiforgery.
- What stops two accepts of one booking? State check, `rowversion`, and a conflict response when the row changed.
- What stops two pending requests for the same provider and service? Filtered unique index `IX_Bookings_CustomerId_ProviderId_ServiceId` where status is Pending.
- What fills the provider's week? Accepting with a start and a duration. Pending requests do not hold the slot. A later reschedule moves or extends it, and an overlap with another Scheduled or InProgress visit returns 409.
- Where are phone numbers shown? On the booking, to the customer, the provider, and an admin. Not on the public provider profile.
- Why is `AverageRating` stored? List and public cards; recomputed in the review transaction.
- Biggest weakness? Local disk documents (not multi-instance). LocalDB on NVMe needed the sector workaround (README) before live SQL proofs.
