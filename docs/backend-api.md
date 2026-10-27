# Phase 1 API contract

| Method | Path | Result |
| --- | --- | --- |
| GET | /health/live | 200 process alive |
| GET | /health/ready | 200 migrated database query works; 503 otherwise |
| GET | /api/v1/system/status | phase, test mode, GHS, implemented/planned features |
| GET | /api/v1/restaurant | restaurant DTO or 404 if not configured |
| GET | /api/v1/menu/categories | category DTO array |
| GET | /api/v1/menu/items?page=1&pageSize=20 | paginated item DTOs; max pageSize 100 |
| GET | /openapi/v1.json | development-only OpenAPI |
| GET | /api/v1/webhooks/whatsapp | Meta test challenge; echoes the challenge only when mode and configured verify token match |
| POST | /api/v1/webhooks/whatsapp | Meta test webhook; validates raw-body HMAC, stores allowlisted text message once, acknowledges signed status events |

Public catalog endpoints contain only catalog data and coarse operational state. No staff, customer, financial, secret, receipt, or webhook information is exposed by reads. The sole write endpoint is the signed, test-mode Meta webhook; it stores a bounded inbox event, not a business order or payment. Staff authentication and policy authorization are prerequisites for later administrative APIs. See [Meta milestone](automation-milestone-03.md).

Validation errors return 400 ProblemDetails, unexpected failures 500 ProblemDetails, unavailable readiness 503. Catalog reads share a process-wide fixed-window 120 requests/minute limit (429 on rejection). Replace it with deployment-aware/per-client limits once trusted proxy identity and authentication are implemented. No detailed DB exception is returned by the health endpoint.

GET menu items exposes integer `priceMinor`, never floating financial calculations. Queries are EF parameterized, no-tracking and bounded/paginated with stable sorting. OpenAPI is generated from real endpoints. Future paths from the brief are deliberately unimplemented rather than returning success stubs.
