# Production gate — not authorized

- [ ] Always-on API and automation; persistent event execution and reconciliation.
- [ ] Phase 1 Neon/Render/GitHub gates verified and all later phase acceptance tests complete.
- [ ] Staff sessions, finance policies, CSRF strategy, trusted proxy settings, role review.
- [ ] WhatsApp production account and signed webhook smoke tests.
- [ ] Explicit Paystack live authorization/activation and environment-specific credentials.
- [ ] Verified receipt privacy, authorized access, retention and deletion.
- [ ] Backup restoration tested, production Neon privileges and isolation reviewed.
- [ ] Monitoring, alerts, audit review, incident response ownership.
- [ ] Duplicate event replay, refunds, cancellation and delayed payment procedures.
- [ ] Dependency/security scans, load tests, privacy/retention policies and staff training.

Free-tier deployment is a test environment and must not accept real customer orders. Removing the startup test-only guard is a separately reviewed production change; never change live mode automatically.
