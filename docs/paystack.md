# Payments plan (Phase 6)

Phase 1 refuses live mode and implements no payment endpoints. No payment credentials are required for local foundation tests. Paystack integration must use its official HTTP API with test keys, unique server-generated references, server-owned totals, raw-body HMAC-SHA512 signature validation using constant-time comparison, and independent verify requests before financial mutation.

Verification must compare success state, reference, transaction ID, integer amount, GHS, test environment, expected attempt/order, order eligibility and prior confirmation. Persist unique provider identifiers and transaction/outbox changes atomically. Redirects never verify payments. Duplicate/delayed/expired/incorrect-amount results must have explicit reconciliation outcomes and tests. Manual screenshots/OCR remain candidates, with independent trusted funds verification or explicit server-restricted simulated approval.

Before implementation, verify current official Paystack API/webhook documentation and pin test fixtures. Do not turn this plan into an AI or n8n financial decision.
