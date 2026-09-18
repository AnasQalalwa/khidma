# Responsive screenshots

Captured 18 September 2026 against Vite (`http://localhost:5173`) proxying to `https://localhost:5001` after LocalDB + seeder. Viewports **360**, **768**, and **1280**. On each page `document.documentElement.scrollWidth <= window.innerWidth`.

Admin tables use `.table-wrap { overflow-x: auto }` so wide grids scroll inside the panel instead of the page. `html`/`body` use `overflow-x: clip`.

Playwright is not a project dependency. PNGs in this folder:

- `home-360.png`, `home-768.png`, `home-1280.png`
- `catalog-360.png`, `catalog-768.png`, `catalog-1280.png`
- `customer-request-detail-360.png`, `customer-request-detail-768.png`, `customer-request-detail-1280.png` — open Plumbing request in Ramallah with two **Pending** offers (`provider1` + `provider4`)
- `provider-requests-360.png`, `provider-requests-768.png`, `provider-requests-1280.png` — `provider1` available feed showing that request
- `booking-detail-360.png`, `booking-detail-768.png`, `booking-detail-1280.png` — customer booking from `concurrency-check.ps1` (`Scheduled`)
- `admin-verifications-360.png`, `admin-verifications-768.png`, `admin-verifications-1280.png` — `provider2` `PendingReview`

Login PNGs were not a Phase 9 deliverable; cookie + CSRF through the proxy was proven by logging in as customer, provider1, and admin.
