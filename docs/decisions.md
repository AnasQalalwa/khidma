# Engineering Decisions

## Authentication

Khidma uses ASP.NET Core Identity with an HttpOnly authentication cookie.

JWT bearer tokens, access tokens, refresh tokens, `localStorage`, and `sessionStorage` are intentionally not used.

The React client restores the current user with `GET /api/auth/me`. Anonymous calls to that endpoint return JSON HTTP 401, not an HTML login redirect.

## CSRF

Cookie authentication requires anti-forgery protection. The API issues a readable `XSRF-TOKEN` cookie from `GET /api/antiforgery/token`. The shared fetch client sends that value in the `X-XSRF-TOKEN` header on POST, PUT, PATCH, and DELETE.

## Same-origin hosting

In production-style execution, ASP.NET Core serves the built React app from `wwwroot`. Development uses the Vite proxy so the browser still treats `/api` as same-origin.
