# Implementation boundary

The active priority is now **n8n automation first**, as requested after the Phase 1 foundation. The existing backend and Angular implementation is retained. Provision/verify automation persistence and hosting, then test one webhook before moving to Groq, Meta and mock-menu agent behavior. Read `testing.md` for what actually ran; file existence is not test evidence.

| Phase | Status and remaining work |
| --- | --- |
| 1 Architecture/setup | Source implemented. GitHub remote is https://github.com/acob1/RestaurantSystem and main was pushed. Automation Neon is configured and verified; business-Neon and Render hosting remain incomplete. |
| 2 Backend foundation | Initial catalog model/migrations/health/OpenAPI available; staff auth and full business model remain. |
| 3 n8n/webhooks | Standalone n8n/PostgreSQL and n8n-only Render configuration prepared; first authenticated development workflow added. The Docker image built; 4 static checks and 15 real runtime checks passed, including encrypted credentials and container replacement. See automation validation evidence. Durable business ingestion/replay and later workflows remain. |
| 4 WhatsApp/AI | Not implemented. Meta test account, Groq adapters, typed allowlisted actions, conversation persistence remain. |
| 5 Menu/cart | Public catalog/seed available; CRUD, image upload, variants, allergens, opening hours, cart, inventory, checkout remain. |
| 6 Payments | Test-mode guard only. Paystack init/signature/verification/reconciliation, manual evidence, independent approvals remain. |
| 7 Admin operations | Foundation overview/catalog/setup available; login, finance, kitchen, customers, reports/settings writes remain. |
| 8 Automation/tracking | Not implemented; notifications/handoff/replay/reporting remain. |
| 9 Full test matrix | Foundation tests provided; all later business/payment/workflow scenarios remain. |
| 10 Production | Not authorized. Always-on hosting, live provider activation, security/recovery/load/retention/monitoring reviews remain. |

No placeholder endpoint claims a successful order/payment. No imported workflow is represented as complete before it has real secured API operations to call. No live payment or customer ordering is enabled.
