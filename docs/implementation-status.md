# Implementation boundary

The active priority is **n8n automation first**, as requested after the Phase 1 foundation. The existing backend and Angular implementation is retained. Automation persistence, Render hosting, the first authenticated development webhook and the separate Groq mock-conversation workflow have been verified on the hosted development instance. Read `testing.md` for what actually ran; file existence is not test evidence.

| Phase | Status and remaining work |
| --- | --- |
| 1 Architecture/setup | Source implemented. GitHub remote is https://github.com/acob1/RestaurantSystem and main was pushed. Automation Neon and n8n Render Free are configured and verified; business-Neon and hosting for the API/dashboard remain incomplete. |
| 2 Backend foundation | Initial catalog model/migrations/health/OpenAPI available; staff auth and full business model remain. |
| 3 n8n/webhooks | The pinned n8n image runs on Render Free against dedicated Neon PostgreSQL. The first authenticated development workflow is published; hosted readiness and authenticated webhook passed after restart and password rotation. Four initial static checks and 15 disposable local runtime checks also passed, including credential encryption and container replacement. Durable business ingestion/replay remains. |
| 4 WhatsApp/AI | The Groq mock-conversation workflow passed local and hosted real Groq checks. Signed ASP.NET Meta test ingress, a PostgreSQL deduplicated inbox, and separate n8n test bridge passed simulated local checks. Actual Meta test-number setup, hosted API/bridge, backend-controlled business actions and conversation persistence remain. |
| 5 Menu/cart | Public catalog/seed available; CRUD, image upload, variants, allergens, opening hours, cart, inventory, checkout remain. |
| 6 Payments | Test-mode guard only. Paystack init/signature/verification/reconciliation, manual evidence, independent approvals remain. |
| 7 Admin operations | Foundation overview/catalog/setup available; login, finance, kitchen, customers, reports/settings writes remain. |
| 8 Automation/tracking | Not implemented; notifications/handoff/replay/reporting remain. |
| 9 Full test matrix | Foundation tests provided; all later business/payment/workflow scenarios remain. |
| 10 Production | Not authorized. Always-on hosting, live provider activation, security/recovery/load/retention/monitoring reviews remain. |

No placeholder endpoint claims a successful order/payment. No imported workflow is represented as complete before it has real secured API operations to call. No live payment or customer ordering is enabled.
