# Security matrix

Statuses:

- **IMPLEMENTED** — enforced in code and covered by automated tests or a deterministic code path.
- **TO VERIFY** — implemented but still needs a manual pass against a running SQL Server instance.

| Control | Status | Notes |
| --- | --- | --- |
| Cookie auth, HttpOnly, no JWT in browser storage | IMPLEMENTED | Identity cookie; React uses `credentials: 'include'` only. |
| Anonymous `/api/auth/me` returns JSON 401, not HTML | IMPLEMENTED | Cookie events write Problem Details. `AuthEndpointsTests`. |
| CSRF on unsafe methods | IMPLEMENTED | `AutoValidateAntiforgeryToken`; client sends `X-XSRF-TOKEN`. |
| Role gates on controllers | IMPLEMENTED | `[Authorize(Roles=...)]` on customer/provider/admin actions. `AuthorizationMatrixTests`. |
| Ownership on request edit/cancel/detail | IMPLEMENTED | Non-owner customer → 403; missing → 404. |
| Provider eligibility hides ineligible detail | IMPLEMENTED | Ineligible → 404. Same query as `/available`. |
| No customer contact/email on provider request or offer list | IMPLEMENTED | Provider DTOs omit email/contact. `EligibilityTests.Provider_CannotSeeCustomerContactBeforeAcceptance`. |
| Contact visible to booking participants after accept | IMPLEMENTED | Booking detail includes counterpart email; customer `DefaultContact` for participants only. Admin can load request detail with customer email. |
| Offer submit requires eligibility + Open + unique non-withdrawn offer | IMPLEMENTED | 404 if ineligible; 409 if duplicate active offer or unique-index race. |
| Withdraw only by owning provider, Pending → Withdrawn | IMPLEMENTED | Other providers 403; illegal state 409. |
| Accept only by request owner | IMPLEMENTED | Other customers 403. |
| Single accepted offer / single booking | IMPLEMENTED | State + rowversion + filtered unique index. Second accept 409. |
| Booking start/complete only by provider owner | IMPLEMENTED | Illegal transitions 409. |
| Booking cancel by a participant, Scheduled only, reason required | IMPLEMENTED | Non-participant 403; other statuses 409. |
| Review only by booking customer, Completed, once, rating 1–5 | IMPLEMENTED | 403 / 409 / 400. Rating recompute in the same transaction. |
| Admin-only stats, provider approval, catalog mutating APIs | IMPLEMENTED | Non-admin 403. |
| Catalog delete blocked when in use | IMPLEMENTED | Category with services or service with providers/requests → 409. |
| Pagination cap (max 50) | IMPLEMENTED | `PageQuery.Normalize`. `PaginationTests`. |
| SPA fallback does not swallow `/api/*` 404s | IMPLEMENTED | `MapFallback("/api/{**slug}")` returns Problem Details 404. |
| No auto-migrate / seed in Production | IMPLEMENTED | Guarded by `IsDevelopment()`. |
| Global exception handler does not leak exception text | IMPLEMENTED | Generic 500 title; no `exception.Message` in the body. |
| Passwords and connection strings not in source | IMPLEMENTED | User secrets for `ConnectionStrings:Default`, `Seed:AdminPassword`, `Seed:DemoPassword`. |
| Concurrent accept under SQL Server | TO VERIFY | SQLite cannot fully emulate the filtered unique index; run two overlapping accepts against LocalDB. |
| HTTPS cookie Secure in production hosting | TO VERIFY | `CookieSecurePolicy.SameAsRequest`; confirm behind a TLS terminator. |
| SQL injection via city/status filters | TO VERIFY | EF parameterized queries; spot-check SQL logs on a real request. |
| XSS in request title/offer message/review comment | TO VERIFY | React text interpolation; confirm no `dangerouslySetInnerHTML`. |
| Admin unapprove of a live provider | TO VERIFY | New offers stop; existing bookings remain. Confirm product expectation. |
