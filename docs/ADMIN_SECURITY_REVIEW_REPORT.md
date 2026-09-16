# Admin / trust / audit security review

Khidma admin monitoring, provider professional verification, operational suspension, and persistent audit logging. Review date: 16 September 2026. Scope is this local pass on `feature/full-project-development`. **Nothing was pushed.**

## Summary

Professional verification (`PendingReview` / `Approved` / `Rejected`) is distinct from operational suspension (`IsSuspended` + reason). Eligibility for new work is a single query: approved, not suspended, matching service, exact city, open request. Documents are private professional proof, not a public portfolio. Audit logs are append-only. Admin users can inspect accounts and last login without seeing passwords.

## Trust model

| Gate | Effect on new work | Effect on existing jobs |
| --- | --- | --- |
| `VerificationStatus != Approved` | Available list is 200 empty; offer submit is 403 | Unchanged |
| `IsSuspended` | Same as above; pending offers are rejected | Bookings stay; an in-progress job can still be completed |
| Missing approved document | Admin cannot approve the provider (409) | N/A |

Admin approval without an approved document is denied and audited. Rejecting a provider or document requires a reason that is stored and shown to the provider.

## Documents

- Allowed types: PDF, JPEG, PNG. Size 1 byte–10 MB.
- Validation: extension, `Content-Type`, and magic bytes (`%PDF`, `FF D8 FF`, `89 50 4E 47 0D 0A 1A 0A`).
- Stored under `ProviderDocuments:RootPath` (default `{ContentRoot}/App_Data/provider-documents`), gitignored. Names are GUIDs. No user-supplied path segments.
- Not served from `wwwroot`. Download is an authorized `File()` result for the owning provider or an admin.
- Delete is allowed only while review status is `Pending`.
- Foreign key to `ProviderProfile` uses `DeleteBehavior.Restrict`.

## Audit

- `AuditLog` has no foreign keys and no PUT/PATCH/DELETE API.
- `IAuditService` captures actor user id/email/role, IP, user agent, and `TraceIdentifier`.
- `DetailsJson` is an explicit dictionary. Passwords, cookies, CSRF tokens, and file bytes are not written.
- Persistence failures are logged and swallowed so audit cannot fail a successful business request.
- Call sites include register/login success and failure, logout, profile, marketplace, verification decisions, suspension, and admin catalog mutations.
- Denied events include: approve provider without an approved document, unauthorized document access, offer while unverified or suspended.

## Authentication and CSRF

- Cookie auth is unchanged (HttpOnly Identity cookie, no JWT in browser storage).
- Unsafe methods still require `X-XSRF-TOKEN`. Missing token → 400 (`CsrfMutationTests`).
- `LastLoginAt` updates only after a successful login. Failed login is audited with the normalized email, never the password.

## Authorization matrix (high level)

| Actor | Own documents | Other provider documents | Admin verification/users/audit | Offer while suspended |
| --- | --- | --- | --- | --- |
| Anonymous | 401 | 401 | 401 | 401 |
| Customer | 403 | 403 | 403 | 403 |
| Other provider | 403 | 403 | 403 | 403 |
| Owner provider | 200 (pending delete allowed) | 403 | 403 | 403 |
| Admin | 200 download/review | 200 | 200 | N/A |

## Admin workspace

Overview, Users (+ detail), Provider Verification (+ document review), Providers (suspend with reason / reactivate), Audit Logs (filters, table, drawer), Catalog. Public provider pages expose `isVerified`, not internal suspension or rejection notes.

## Residual risk / TO VERIFY

1. **SQL Server concurrency** — filtered unique index on accepted offers is not fully emulated on SQLite. Run `scripts/concurrency-check.ps1` against LocalDB when it is available.
2. **Live smoke** — `scripts/smoke-test.ps1` was not run; LocalDB failed to start in this environment.
3. **Production TLS cookies** — `CookieSecurePolicy.SameAsRequest` still needs confirmation behind a TLS terminator.
4. **XSS** — React interpolates request titles, offer messages, review comments, and admin notes. Confirm there is still no `dangerouslySetInnerHTML` on those fields.
5. **Document malware** — magic-byte checks stop extension spoofing, not PDF/JPEG exploit payloads. Treat uploads as untrusted blobs; do not execute them.
6. **Audit volume** — logs grow without a retention job. That is acceptable for this MVP; production should add archival later, never silent deletes from the admin UI.
7. **LocalDB** — `dotnet ef database update` was not applied here. Apply `AddProviderVerificationAuditAndSuspension` before using the new APIs against SQL Server.

## Verification performed

See `docs/TEST_RESULTS.md`. Backend 109 / frontend 25 automated tests passed. `has-pending-model-changes` reported a clean model. Secret scan found no committed live secrets.

## Conclusion

The admin trust layer is enforced in code and covered by automated tests for the rules that SQLite can express. Remaining gaps are environment-bound (LocalDB down, live smoke, SQL Server race) rather than missing product controls. Do not enable public document URLs, do not reintroduce a boolean `IsApproved` toggle, and do not add audit delete.
