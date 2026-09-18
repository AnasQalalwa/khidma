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
| 16 | New provider registers | `VerificationStatus = PendingReview`; dashboard shows verification banner; available list empty; offer 403 | Yes — registration + eligibility tests |
| 17 | Admin approves a document then the provider | Provider appears in matching available requests | Yes — `ProviderVerificationDecisionTests` + `AdminTests` |
| 18 | Admin catalog: duplicate name, delete category with services | 409 | Yes — `AdminTests` |
| 19 | Role matrix: anonymous POST request, provider hitting `/mine`, customer hitting `/available` | 401 / 403 / 403 | Yes — `AuthorizationMatrixTests` |
| 20 | Pagination `pageSize=99` | Capped to 50 (audit logs cap at 100) | Yes — `PaginationTests` |
| 21 | Frontend: login error, wrong-role redirect, empty requests, offer submit, ApiError empty/error states, booking actions, rating required | UI matches | Yes — Vitest |
| 22 | Provider uploads PDF/JPEG/PNG; exe, fake signature, and oversize rejected | 200 / 400 | Yes — `VerificationDocumentTests` |
| 23 | Document access matrix (own / other provider / customer / anon / admin) | 200 / 403 / 401 | Yes — `VerificationDocumentTests` |
| 24 | Approve provider with 0 / pending / rejected docs | 409 until one document is Approved | Yes — `ProviderVerificationDecisionTests` |
| 25 | Admin suspends an approved provider | Pending offers rejected; bookings untouched; available empty; offer 403 | Yes — `SuspensionTests` |
| 26 | Suspended provider can complete an InProgress booking | 200, booking Completed | Yes — `SuspensionTests` |
| 27 | Reactivate a suspended approved provider | Eligible again; suspension fields cleared | Yes — `SuspensionTests` |
| 28 | Audit events for auth, verification, suspension; no password/cookie/XSRF/binary in details | Listed actions present; secrets absent; no DELETE route | Yes — `AuditLogTests` |
| 29 | Admin user list/filter/search and `LastLoginAt` on successful login only | DTO populated; failed login leaves LastLoginAt null | Yes — `AdminUserMonitoringTests` |
| 30 | CSRF: missing token 400, valid token succeeds | 400 / 200 | Yes — `CsrfMutationTests` |
| 31 | Full marketplace workflow including verification | Request → offer → accept → complete → review + audit | Yes — `FullMarketplaceWorkflowTests` |
| 32 | Frontend admin users, verification queue/detail, providers suspend/reactivate, audit filters/drawer, provider banners/documents | UI matches | Yes — Vitest admin/provider tests |

Manual (SQL Server / browser) — not replaced by SQLite tests:

- [ ] Full UI pass of admin Overview, Users, Verification, Providers, Audit Logs at 360 / 430 / 768 / 1024 / 1280 / 1440.
- [ ] Confirm cookie + CSRF through the Vite proxy (`http://localhost:5173` → `https://localhost:5001`).
- [x] Confirm SPA fallback: refresh `/customer/requests/1` on the API host still serves the React app. (Automated: `SpaFallbackTests`. Re-check in the browser after SQL Server is up.)
- [ ] Concurrent accept against SQL Server (`scripts/concurrency-check.ps1`).
- [ ] Live smoke against LocalDB (`scripts/smoke-test.ps1`).
- [ ] Upload a real PDF in the provider profile and download it as admin.
- [ ] Suspend a provider in the UI and confirm pending offers flip to Rejected.