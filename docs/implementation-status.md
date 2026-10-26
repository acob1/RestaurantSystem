# Implementation boundary

Phase 1 is the current authorized build slice, following the brief's instruction to establish the first milestone before later integrations. Read `testing.md` for what actually ran; file existence is not test evidence.

| Phase | Status and remaining work |
| --- | --- |
| 1 Architecture/setup | Source implemented. External Neon/GitHub/Render account provisioning and hosted acceptance remain account-dependent. |
| 2 Backend foundation | Initial catalog model/migrations/health/OpenAPI available; staff auth and full business model remain. |
| 3 n8n/webhooks | Docker/DB/deployment configuration prepared; durable ingestion, dispatch, fourteen actual workflows and recovery remain. |
| 4 WhatsApp/AI | Not implemented. Meta test account, Groq adapters, typed allowlisted actions, conversation persistence remain. |
| 5 Menu/cart | Public catalog/seed available; CRUD, image upload, variants, allergens, opening hours, cart, inventory, checkout remain. |
| 6 Payments | Test-mode guard only. Paystack init/signature/verification/reconciliation, manual evidence, independent approvals remain. |
| 7 Admin operations | Foundation overview/catalog/setup available; login, finance, kitchen, customers, reports/settings writes remain. |
| 8 Automation/tracking | Not implemented; notifications/handoff/replay/reporting remain. |
| 9 Full test matrix | Foundation tests provided; all later business/payment/workflow scenarios remain. |
| 10 Production | Not authorized. Always-on hosting, live provider activation, security/recovery/load/retention/monitoring reviews remain. |

No placeholder endpoint claims a successful order/payment. No imported workflow is represented as complete before it has real secured API operations to call. No live payment or customer ordering is enabled.
