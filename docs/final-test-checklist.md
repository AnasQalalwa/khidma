# Final test checklist

Use seeded accounts (`customer@khidma.local` + `provider1@khidma.local` in Ramallah, Plumbing) unless a scenario says otherwise. Passwords come from user secrets, never from git.

| # | Scenario | Expected | Automated? |
| --- | --- | --- | --- |
| 1 | Customer creates a valid service request | 200, status `Open`, appears on My requests | Yes — `ServiceRequestTests` |
| 2 | Preferred date in the past / invalid budget | 400 validation problem | Yes — `ServiceRequestTests` |
| 3 | Approved provider in the same city with the matching service sees the request | Appears on `/available`; detail 200 | Yes — `EligibilityTests` |
| 4 | Wrong city, wrong service, unapproved, or cancelled request | Not listed; ineligible detail is 404 | Yes — `EligibilityTests` |
| 5 | Provider submits a valid offer | 200, status `Pending`; customer sees it without provider email | Yes — `OfferTests` |
| 6 | Second non-withdrawn offer by the same provider | 409 | Yes — `OfferTests` |
| 7 | Provider withdraws a Pending offer | 200, `Withdrawn`; can submit again | Yes — `OfferTests` |
| 8 | Customer accepts one of two offers | Booking `Scheduled`; sibling `Rejected`; request `Booked` | Yes — `AcceptOfferTests` |
| 9 | Second accept on the same request | 409 | Yes — `AcceptOfferTests` |
| 10 | Non-owner customer tries to accept | 403 | Yes — `AcceptOfferTests` |
| 11 | Provider starts then completes | `Scheduled` → `InProgress` → `Completed`; request `Completed` | Yes — `BookingTests` |
| 12 | Illegal booking transitions (complete while Scheduled, start while Completed, cancel while InProgress) | 409 | Yes — `BookingTests` |
| 13 | Participant cancels a Scheduled booking with a reason | Booking and request `Cancelled` | Yes — `BookingTests` |
| 14 | Customer reviews a completed booking (1–5) | Review stored; `AverageRating` / `ReviewCount` recomputed | Yes — `ReviewTests` |
| 15 | Review when not completed, duplicate review, wrong customer, rating 6 | 409 / 409 / 403 / 400 | Yes — `ReviewTests` |
| 16 | New provider registers | `IsApproved = false`; dashboard shows pending banner; no available requests | Yes — registration + eligibility tests |
| 17 | Admin approves the provider and the provider sets services | Provider appears in available matching requests | Yes — `AdminTests` + `ProviderProfileTests` |
| 18 | Admin catalog: duplicate name, delete category with services | 409 | Yes — `AdminTests` |
| 19 | Role matrix: anonymous POST request, provider hitting `/mine`, customer hitting `/available` | 401 / 403 / 403 | Yes — `AuthorizationMatrixTests` |
| 20 | Pagination `pageSize=99` | Capped to 50 | Yes — `PaginationTests` |
| 21 | Frontend: login error, wrong-role redirect, empty requests, offer submit, ApiError empty/error states, booking actions, rating required | UI matches | Yes — Vitest (11 cases) |

Manual (SQL Server / browser) — not replaced by SQLite tests:

- Full UI pass of the 21 scenarios at 360 / 430 / 768 / 1024 / 1280 / 1440.
- Confirm cookie + CSRF through the Vite proxy (`http://localhost:5173` → `https://localhost:5001`).
- Confirm SPA fallback: refresh `/customer/requests/1` on the API host still serves the React app.
- Concurrent accept against SQL Server (two tabs).
