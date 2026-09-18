# Responsive screenshots

Viewports to capture: **360**, **768**, and **1280**. Check `document.documentElement.scrollWidth <= window.innerWidth` on each page.

Pages: Home, Catalog, Login, customer request detail, provider requests, booking detail, admin users.

Admin tables use `.table-wrap { overflow-x: auto }` so wide grids scroll inside the panel instead of the page. `html`/`body` use `overflow-x: clip`.

Playwright is not a project dependency. Capture these PNGs here once LocalDB (or SQL Server 2022) is running and the HTTPS API is up:

- `home-360.png`, `home-768.png`, `home-1280.png`
- `catalog-360.png`, `catalog-768.png`, `catalog-1280.png`
- `login-360.png`, `login-768.png`, `login-1280.png`
- `customer-request-detail-360.png`, `customer-request-detail-768.png`, `customer-request-detail-1280.png`
- `provider-requests-360.png`, `provider-requests-768.png`, `provider-requests-1280.png`
- `booking-detail-360.png`, `booking-detail-768.png`, `booking-detail-1280.png`
- `admin-users-360.png`, `admin-users-768.png`, `admin-users-1280.png`

Live capture in this closeout was blocked: SQL Server LocalDB fails to start (`256 misaligned log IOs`). Public pages can still be checked against a Vite preview; authenticated dashboards need the API.
