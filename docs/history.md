# Project history

Week-by-week record for reviewers. Day-to-day file lists from early reports were deleted; this is the durable summary.

## Week 1 — Auth vertical slice

Branch work from `develop`: Identity cookie auth, CSRF (`X-XSRF-TOKEN`), register/login/logout/`/me`, catalog read APIs, React auth shell, SPA hosted from `wwwroot`. Tests via `WebApplicationFactory` + SQLite. Completion narrative that used to live in `WEEK1_COMPLETION_REPORT.md` is reduced to this paragraph.

## Week 2–3 — Marketplace

Customer requests, provider eligibility (exact city), offers, transactional accept, bookings, reviews, stored rating aggregates, role dashboards, admin catalog. Later: verification status instead of `IsApproved`, professional documents, suspension, append-only audit, admin users/monitoring. UI redesign of home, catalog, auth, and workspaces.

Implementation dump formerly in `FINAL_IMPLEMENTATION_REPORT.md` and the frontend notes in `REVIEW_HANDOFF.md` are superseded by `docs/decisions.md`, the root README, and the test suite.

## Week 4 — Closeout (`feature/full-project-development`)

- Baseline gate recorded in `docs/TEST_RESULTS.md` (109 backend / 25 frontend, then growing).
- Accept-race 409 unified; SQL Server opt-in tests (skipped until LocalDB starts).
- Security matrix walk, CSRF stays 400 (ADR 17), ineligible URL stays 404.
- Controllers thinned; `TreatWarningsAsErrors`; EF command logging in Development.
- Frontend strict TypeScript, field errors, 409 reload, EmptyState.
- ADRs 19–21 for uploads, suspension, audit.
- `deploy/migrate.sql`, HSTS, secure cookies in Production, generic 500s, Azure runbook (cloud steps stopped for the operator).
- Plan of record moved to `docs/plan-v2.md`. Seeder adds `provider4@khidma.local` for the two-offer demo.

## Admin trust review (folded from `ADMIN_SECURITY_REVIEW_REPORT.md`)

Professional verification is distinct from operational suspension. Eligibility is one query. Documents are private proof (PDF/JPEG/PNG, magic bytes, 10 MB, GUID names, not `wwwroot`). Audit is append-only and swallows write failures. Residual risks that remain environment-bound: LocalDB IO alignment, live smoke, concurrent accept on real SQL Server, no malware scan on uploads, no audit retention job.
