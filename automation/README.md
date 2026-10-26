# n8n first: one webhook at a time

Existing Angular, ASP.NET Core, business migrations and dashboard are retained. This milestone starts **only automation**: a dedicated PostgreSQL database, pinned n8n Community Edition, one authenticated development inbox, and real HTTP/redeployment tests. Groq and WhatsApp come after this checkpoint.

## Local start

Use Node 24.15 or a compatible Node 24 release. From the repository root:

```sh
export PATH="$PWD/.tools/node/bin:$PATH"
node automation/scripts/setup-local.mjs
node automation/scripts/start-local.mjs
```

`setup-local` creates an ignored, mode-0600 file only if missing. It never replaces the encryption key or webhook token. The local start/bootstrap helpers explicitly prefer these private settings over ambient shell values, so another project’s database variables or the older root dev helper cannot silently replace the encryption key. The separate Compose project `restaurant-automation` does not modify the existing `docker-business-db-1` service or volume. If port 5678 is busy, change `AUTOMATION_PORT` in this automation-only file.

Install this one workflow automatically into the dedicated local database:

```sh
node automation/scripts/bootstrap-local.mjs
node automation/scripts/check-local.mjs
```

The bootstrap refuses to replace an existing workflow or credential, uses the generated private header token, and never connects to Neon. It stops the n8n server while its administrative imports run, then starts it again. This avoids running the server and CLI together under a small memory budget. It creates no owner password.

Open http://localhost:5678 and create your own n8n owner account. No development owner password is committed or auto-assigned to your lasting instance. Health endpoints are `/healthz` and `/healthz/readiness`.

For a different instance, or if you prefer manual setup, import `automation/workflows/01-development-inbox.json` in the editor. In **Development inbox**, select a **Header Auth** credential named `Restaurant development webhook`, header name `X-Restaurant-Webhook-Token`, and value from `RESTAURANT_WEBHOOK_TOKEN` in your private env file. Save the credential and publish this one workflow. Imported credential references are placeholders; select your actual credential after importing in a different instance.

After publishing, run `node automation/scripts/check-local.mjs` from the repository root. It reads the private token without printing it and checks the real local webhook. For a local instance connected to Neon, add `--env-file automation/.env.neon`.

Use the production-style URL `/webhook/restaurant-dev/inbox` for repeatable testing. “Production URL” is n8n terminology for a published webhook; this workflow remains development-only. `/webhook-test/…` works only while the editor listens for a test event.

## Webhook contract

POST a JSON body with `messageId` (1–80 ASCII letters/numbers/underscores/hyphens), `customerId` prefixed `mock-`, and `text` (1–1000 characters). Include the configured header token. Fixtures are in `tests/fixtures/`. Successful requests return HTTP 200, the original message ID, `mode: development`, and a fixed acknowledgement. Invalid inputs return 422; missing/incorrect auth is rejected. The global JSON body cap is 1 MB. In pinned n8n 2.44.7, exceeding it produces HTTP 500 with a generic error, while the server logs `PayloadTooLargeError`. The integration test verifies that specific rejection and confirms the next normal request still succeeds. Do not interpret an arbitrary HTTP 500 as evidence that the limit works.

Repeated IDs produce the same acknowledgement. This first workflow is stateless and makes **no business writes**; this is repeat-safe acknowledgement, not a durable inbox or deduplicated AI conversation. There is no order, payment, menu lookup, or outbound WhatsApp action yet. Message text never becomes executable workflow instructions. Execution payload retention is disabled.

## Tests

```sh
node --test automation/tests/workflow.test.mjs
node automation/scripts/integration-test.mjs
```

The integration script builds the actual pinned Dockerfile, creates uniquely named disposable PostgreSQL and n8n containers, imports a protected credential through stdin, imports/publishes the workflow, and sends real HTTP requests. It tests auth, validation, repeated messages, size limits, encrypted credential persistence, and a completely new application container/user folder with the same PostgreSQL database and encryption key. It enforces a 512 MiB memory cap for the n8n container. It removes only its owned disposable resources. It does not read your Neon variables or use your lasting local database. Results are written without secrets to ignored `artifacts/automation/milestone-01.json`.

The workflow never saves execution bodies. The export contains credential references only. Keep your actual encryption key stable and backed up in a secret manager. Replacing it can make all stored credentials unreadable.

## Neon persistence

Install/connect the Neon integration to provision and verify a development project from this chat, or use your Neon Console:

1. Select a **development** project separate from business/staging/production. Create `restaurant_automation_dev` and dedicated role `restaurant_automation`. The role may own this dedicated database, or have CONNECT on it plus USAGE and CREATE on its public schema. The optional psql script `database/create-neon-database.sql` creates only those new resources and prompts for the password. Run it once as an authorized project owner; it deliberately fails on existing names instead of replacing them.
2. Copy `automation/neon.env.example` to ignored `automation/.env.neon`. Fill database credentials from your Neon connection panel. Use the **direct** hostname (without `-pooler`) because n8n performs its own migrations. Its application pool is limited to 2.
3. Set a random stable encryption key and webhook token. Use a separate Compose project/volume. Do not copy or overwrite the existing business connection settings.
4. Start n8n without the `local-db` profile:

   ```sh
   node automation/scripts/start-neon.mjs
   ```

5. Confirm `/healthz/readiness`, owner setup and the first authenticated webhook. Recreate **only this n8n container** and confirm workflows/credentials persist. Verify the new Neon database contains n8n-managed tables and the business database is unchanged. No EF migration runs here.

For the existing dedicated database, these grants can be run by its owner in the Neon SQL Editor with `restaurant_automation_dev` selected:

```sql
GRANT CONNECT ON DATABASE restaurant_automation_dev TO restaurant_automation;
GRANT USAGE, CREATE ON SCHEMA public TO restaurant_automation;
```

The private Neon file can use `AUTOMATION_PORT=5679` so the original local workspace remains on 5678. After entering the database password locally:

```sh
node automation/scripts/check-neon.mjs
node automation/scripts/start-neon.mjs
node automation/scripts/bootstrap-neon.mjs
node automation/scripts/check-local.mjs --env-file automation/.env.neon --timeout-ms 60000
```

The check is read-only and verifies the real client TLS socket, certificate authorization and schema permissions. The initial cloud schema setup can take several minutes; the startup helper waits up to ten minutes. `node automation/scripts/verify-neon-restart.mjs` additionally verifies the authenticated webhook before/after container recreation and confirms the original local instance remains unchanged. The start command uses a separate Compose project and lets n8n manage its own schema migrations. Bootstrap adds just the first workflow and encrypted Header Auth credential; existing records are retained. No database is dropped or reset. Open http://localhost:5679 to finish owner setup if prompted.

Before starting a Render n8n service against this same Neon database, stop the local **Neon-backed** instance to avoid running two main n8n processes on one database. Retain the same encryption key in Render. The original 5678 workspace uses a separate database and can stay running. Use:

```sh
docker compose --env-file automation/.env.neon -f automation/compose.yml stop n8n
```

Neon SSL is enabled with certificate validation; never set reject-unauthorized false. No real Neon connection is implied by passing tests against local PostgreSQL.

## Render: n8n only

Select `automation/render.yaml` as the Blueprint path for `https://github.com/acob1/RestaurantSystem`. The original three-service Blueprint remains available, but this one prepares only `restaurant-n8n` on Free. Keep automatic deployment off and use the reviewed main branch.

The Blueprint selects Frankfurt to match the existing Neon project. Set the dedicated Neon host/database/user/password and **the exact same** `N8N_ENCRYPTION_KEY` used by the local Neon-backed instance. Set `N8N_HOST` to the assigned Render hostname, and `N8N_WEBHOOK_URL` and `N8N_EDITOR_BASE_URL` to its HTTPS origin ending `/`. Port is 10000, listen address is 0.0.0.0, proxy hops 1, SSL verification true. The owner account, published workflow, and encrypted Header Auth credential already in this Neon database should appear in Render; sign in with the owner account you just created. Do **not** import the workflow or create a second credential unless a read-only database check proves they are absent. Do not expose the editor password, credential secret or encryption key in GitHub/Angular.

For a hosted smoke check, supply the Render HTTPS origin and read the existing token from your ignored private Neon file without printing or uploading the file:

```sh
N8N_BASE_URL=https://YOUR-RENDER-HOST.onrender.com node automation/scripts/check-remote.mjs --env-file automation/.env.neon
```

The test waits up to five minutes for readiness and up to 60 seconds for one simulated webhook response. Separately restart the service and verify that the credential still decrypts and the published webhook still responds. Render Free has ephemeral storage, sleep/cold-start delays, and resource limits; use it for development only. Local testing under 512 MiB is not evidence of Render stability. No paid upgrade or live order handling is enabled.

## Next checkpoints

1. Verify actual Neon and Render health/persistence. Local substitute checks do not satisfy these external checks.
2. After the webhook passes, add Groq through a protected credential and test its real developer API, timeout/rate-limit handling, and typed response validation. Never put the key in a workflow export or `$env` expression.
3. Connect Meta's WhatsApp test sender/allowlisted recipient. Verify the webhook handshake and raw-body signature before processing messages. Use one private test recipient and never connect live customer ordering here.
4. Extend this tested workflow incrementally into a mock-menu restaurant agent for greetings, menu enquiries and tentative order summaries. Mock prices must be fixed data, and responses must make clear that no real order/payment is confirmed.
5. Return to API-backed menus, checkout, Paystack, Cloudinary and dashboard features after those checks pass.

References: [PostgreSQL variables](https://docs.n8n.io/hosting/configuration/environment-variables/database/), [webhook authentication](https://docs.n8n.io/integrations/builtin/core-nodes/n8n-nodes-base.webhook/), [publishing from CLI](https://docs.n8n.io/hosting/cli-commands/).
