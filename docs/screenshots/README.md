# Responsive screenshots

Captured against Vite (`http://localhost:5173`) proxying to `https://localhost:5001` after LocalDB + seeder. Viewports **360**, **768**, and **1280** at `deviceScaleFactor: 1` via Playwright Chromium (`npm run screenshots` in `client/`). Not part of CI.

On each page `document.documentElement.scrollWidth <= window.innerWidth`.

Admin tables use `.table-wrap { overflow-x: auto }` so wide grids scroll inside the panel instead of the page. `html`/`body` use `overflow-x: clip`.

```powershell
# API:  dotnet run --project server/Khidma.Api --launch-profile https
# Vite: npm run dev   (from client/)
$env:KHIDMA_ADMIN_EMAIL = 'admin@khidma.local'
$env:KHIDMA_CUSTOMER_EMAIL = 'customer@khidma.local'
$env:KHIDMA_PROVIDER_EMAIL = 'provider1@khidma.local'
# passwords from Seed:* user-secrets — never commit them
cd client
npx playwright install chromium
npm run screenshots
```

PNGs in this folder:

- `home-360.png`, `home-768.png`, `home-1280.png`
- `catalog-360.png`, `catalog-768.png`, `catalog-1280.png`
- `customer-request-detail-360.png`, `customer-request-detail-768.png`, `customer-request-detail-1280.png` — open Plumbing request in Ramallah with two **Pending** offers (`provider1` + `provider4`)
- `provider-requests-360.png`, `provider-requests-768.png`, `provider-requests-1280.png` — `provider1` available feed showing that request
- `booking-detail-360.png`, `booking-detail-768.png`, `booking-detail-1280.png` — customer booking (`Scheduled`)
- `admin-verifications-360.png`, `admin-verifications-768.png`, `admin-verifications-1280.png` — `provider2` `PendingReview`
- `admin-overview-360.png`, `admin-overview-768.png`, `admin-overview-1280.png` — admin platform overview at 7 days
- `admin-audit-360.png`, `admin-audit-768.png`, `admin-audit-1280.png` — audit log list with humanized summaries
