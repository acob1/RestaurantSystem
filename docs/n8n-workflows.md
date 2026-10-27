# n8n automation: current checkpoint

The development order is now automation first. [automation/README.md](../automation/README.md) contains the executable local/Neon/Render setup and first webhook contract. Existing business database, .NET API and Angular files are retained.

The image remains pinned at Community Edition 2.44.7. The standalone Compose project uses its own PostgreSQL service and volumes; the hosted n8n service uses a dedicated Neon database with verified TLS. `automation/render.yaml` deploys only n8n on Render Free, leaving the existing three-service Blueprint intact.

`01-development-inbox.json` remains the hosted, published first workflow. It uses supported Webhook, If and Respond to Webhook nodes, Header Auth stored as an encrypted n8n credential, strict simulated-message validation, bounded payloads, and explicit success/error responses. Message bodies are not retained. It makes no business writes or outbound provider calls; repeated IDs return the same acknowledgement.

`02-ai-conversation.json` is a separate workflow, now published on hosted n8n. It uses an encrypted Groq Bearer Auth credential, a reviewable system prompt, and fixed mock Ghanaian menu data. It validates the model's structured intent/SKUs and computes demo prices in minor units. It makes no business write or outbound WhatsApp call. See [milestone 02](automation-milestone-02.md) for import steps, local tests and the hosted real Groq checks.

`03-meta-test-bridge.json` is a separate inactive-on-import test workflow. It accepts authenticated dispatch from the ASP.NET signed Meta callback, calls workflow 02, and can use Meta's test number to reply only to one configured recipient. It has been tested with disposable fake Groq and fake Meta servers; it is **not** published on the hosted instance or connected to a real Meta test number. See [milestone 03](automation-milestone-03.md).

The integration test creates disposable PostgreSQL/n8n containers, imports/publishes this workflow, exercises real HTTP, and replaces the application container/user folder to verify PostgreSQL and encryption-key persistence. On Render, readiness and the authenticated webhook passed before and after an explicit service restart. See [automation evidence](automation-milestone-01.md) for actual results and account-dependent limits.

Next gate: configure business Neon and the API test deployment, then connect Meta's test number and verify one signed inbound and outbound exchange. No bulk workflow generation or live customer/payment processing is enabled.

References: [database configuration](https://docs.n8n.io/hosting/configuration/environment-variables/database/), [Webhook](https://docs.n8n.io/integrations/builtin/core-nodes/n8n-nodes-base.webhook/), [Server CLI](https://docs.n8n.io/hosting/cli-commands/).
