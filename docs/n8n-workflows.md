# n8n automation: current checkpoint

The development order is now automation first. [automation/README.md](../automation/README.md) contains the executable local/Neon/Render setup and first webhook contract. Existing business database, .NET API and Angular files are retained.

The image remains pinned at Community Edition 2.44.7. The new standalone Compose project uses its own PostgreSQL service and volumes; it can alternatively use a dedicated Neon database with verified TLS. `automation/render.yaml` prepares only n8n on Render Free, leaving the existing three-service Blueprint intact.

`01-development-inbox.json` is the only exported workflow. It uses supported Webhook, If and Respond to Webhook nodes, Header Auth stored as an encrypted n8n credential, strict simulated-message validation, bounded payloads, and explicit success/error responses. Message bodies are not retained. It makes no business writes or outbound provider calls; repeated IDs return the same acknowledgement. This is not a durable conversation/inbox implementation.

The integration test creates disposable PostgreSQL/n8n containers, imports/publishes this workflow, exercises real HTTP, and replaces the application container/user folder to verify PostgreSQL and encryption-key persistence. See [automation evidence](automation-milestone-01.md) for actual results and account-dependent limits.

Next gates: verify real Neon/Render persistence; test Groq with protected credentials and failure handling; connect signed Meta test ingress; evolve one workflow into a mock-menu agent; then restore the original business integration sequence. No bulk workflow generation or live customer/payment processing is enabled.

References: [database configuration](https://docs.n8n.io/hosting/configuration/environment-variables/database/), [Webhook](https://docs.n8n.io/integrations/builtin/core-nodes/n8n-nodes-base.webhook/), [Server CLI](https://docs.n8n.io/hosting/cli-commands/).
