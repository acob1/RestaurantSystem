# Security status and gates

Implemented: test-only payment startup guard; hosted database TLS VerifyFull; bounded connection pool; no secret in Angular; typed DTOs; no administrative writes; explicit catalog pagination validation; ProblemDetails; process-wide request rate limiting; exact-origin CORS; nosniff/referrer headers; relational category/price constraints; deterministic development-only seeds; isolated PostgreSQL test databases. Meta test ingress now checks the GET verify token and POST raw-body HMAC, enforces one test-number/recipient allowlist, and deduplicates IDs in PostgreSQL. The n8n Meta bridge remains inactive on hosted n8n until test credentials are configured.

Not yet implemented: staff authentication/session invalidation; finance policies; CSRF (if cookies chosen); signed payment webhook ingestion; idempotent financial transactions; trusted WhatsApp media fetching; Cloudinary authenticated evidence; retention/deletion jobs; audit trails; full prompt-injection tool allowlists; reconciliation/refunds; verified backup restores; load/penetration tests. These are acceptance gates in subsequent phases, not completed protections.

Phase 2 should use secure HTTP-only cookies or a carefully defined bearer lifecycle. A cross-site cookie design must explicitly solve CSRF, SameSite, credentialed exact CORS, session expiry and logout invalidation; do not add localStorage JWT shortcuts. Configure trusted forwarding proxies before interpreting HTTPS/client IP, never trust arbitrary forwarded headers.

No live switch exists in this milestone. The application refuses `Payments__AllowLivePayments=true` or a Paystack mode other than Test at startup in every environment. Later adapters must additionally refuse non-`sk_test_` credentials until production is explicitly authorized. API operational status never reflects a successful charge.

Logs should contain event/reference identifiers and failure types, not message bodies, phone numbers, credentials, connection strings, or receipts. Dependency audit commands in CI are initial checks; patch review and an update cadence are still required.
