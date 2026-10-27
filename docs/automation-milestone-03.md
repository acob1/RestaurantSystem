# Milestone 3 — Meta WhatsApp test integration

Status on 2026-10-27: **local signed-ingress and simulated bridge checks passed**. No Meta account, real test number, hosted API, or hosted workflow 03 has been connected yet. Workflows 01 and 02 were not edited. Do not point Meta at the n8n webhook: the public Meta callback is the ASP.NET Core endpoint, which verifies the signature before inserting a durable message.

## Flow and boundary

`Meta test number → ASP.NET GET challenge / signed POST → business PostgreSQL MetaWebhookEvents → test dispatch worker → authenticated n8n workflow 03 → existing workflow 02 / Groq → Meta test-number /messages API → one allowlisted recipient.`

The API compares the GET verify token, verifies `X-Hub-Signature-256` over the original POST bytes using the App Secret, limits payload size, accepts only text from one configured test recipient and phone-number ID, and inserts each Meta message ID once with a unique PostgreSQL constraint. Signed status events and unsupported media are acknowledged without processing. Immediate Graph API send failures are classified, but later asynchronous delivery-status failures are **not yet tracked**. Workflows 01 and 02 remain separate. Workflow 03 has its own Header Auth credential, calls workflow 02 through its existing protected webhook, validates the AI reply, and uses a separate Bearer Auth credential for the Meta Graph API. Mock prices remain demonstrative; no cart, order or payment is created.

The worker claims a pending event atomically. A successful Graph API acceptance marks it `MetaAccepted` (not proof that the recipient received it); a definite failure marks it `Failed`; a timeout or ambiguous result marks it `Unknown`. Claimed/Unknown events are **not automatically retried**, because Graph message creation is not assumed idempotent. This avoids a duplicate reply but can lose a reply if the process stops after claiming. Inspect and resolve those rows manually in development. Render Free sleep and cold starts can delay or miss webhook delivery; this design is test-only.

## Files and configuration

- ASP.NET ingress: `backend/src/Restaurant.API/MetaWebhookEndpoints.cs`.
- Durable worker: `backend/src/Restaurant.API/MetaTestDispatchWorker.cs`.
- Business migration: `backend/src/Restaurant.Infrastructure/Migrations/*_MetaTestInbox.cs`.
- Separate importable workflow: `automation/workflows/03-meta-test-bridge.json`; source builder: `automation/scripts/build-meta-workflow.mjs`.
- Separate Render API Blueprint: `backend/render.yaml`. The existing automation Blueprint is unchanged.

API environment variables for the **test** service:

| Name | Value/source |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | Separate business Neon PostgreSQL, pooled runtime hostname, Npgsql format, `SSL Mode=VerifyFull`. Never use the n8n database. |
| `Meta__TestMode` | `true`; setting it to `false` disables both callback methods and the worker. |
| `Meta__VerifyToken` | A new random value you choose. Enter the same value in Meta Webhooks configuration. |
| `Meta__AppSecret` | App Dashboard → App settings → Basic → App secret; used only for POST signature verification. |
| `Meta__PhoneNumberId` | Numeric **test phone number ID**, not its visible phone number or WABA ID. |
| `Meta__AllowedTestRecipient` | One verified personal recipient's WhatsApp ID/digits with country code, no `+`. |
| `Automation__MetaBridgeUrl` | `https://restaurant-n8n-5i1u.onrender.com/webhook/restaurant-dev/meta-bridge` |
| `Automation__MetaBridgeToken` | Separate random secret; same value goes in the `Restaurant Meta Bridge` n8n Header Auth credential. |
| `Paystack__Mode`, `Payments__AllowLivePayments` | `Test`, `false`. |

Do not place the App Secret, verify token, bridge token or temporary Graph access token in GitHub, Angular, workflow JSON, or chat. The Meta Graph access token belongs **only** in hosted n8n's `Restaurant Meta Test API` Bearer Auth credential. The bridge Header Auth credential name is `Restaurant Meta Bridge` and its header name is exactly `X-Restaurant-Meta-Bridge-Token`.

## Tests actually run

Commands from the repository root using the bundled runtimes:

```sh
DOTNET_CLI_HOME=/private/tmp/restaurant-dotnet .tools/dotnet/dotnet test backend/tests/Restaurant.IntegrationTests/Restaurant.IntegrationTests.csproj --no-restore -m:1 -p:UseSharedCompilation=false
.tools/node/bin/node --test automation/tests/*.test.mjs
.tools/node/bin/node automation/scripts/meta-integration-test.mjs
```

Results: **16/16 ASP.NET integration tests** against disposable PostgreSQL, including the GET challenge, bad/missing signatures, changed raw body, wrong test sender/number, duplicate message IDs, database migration and a single worker dispatch after duplicate delivery. **18/18 n8n export/Code-node tests** passed. **11/11 disposable n8n/PostgreSQL runtime checks** passed with fake Groq and fake Meta HTTP services: the three workflows imported/published together, greeting response reached the fake Graph endpoint, missing bridge authentication and unallowlisted recipients were rejected, Graph 429 and expired-token errors were classified, AI rate limiting blocked outbound send, and the fake Meta token was encrypted in PostgreSQL. The runtime script removed its test containers and volume. These are **not** claims of a real Meta test-number exchange or hosted API readiness.

## Manual setup: business Neon, Render API, then Meta Console

### Rotate an exposed temporary Meta token first

1. Keep workflow 03 unpublished while rotating. In Meta for Developers → your app → WhatsApp → API Setup / Getting Started, generate a fresh **temporary access token** for the same test app. These user tokens expire after about 24 hours. Generating a replacement must **not** be assumed to revoke the exposed token.
2. Inspect the exposed token's validity privately with Meta's Access Token Debugger. If Meta offers a revoke/remove control for that token or its app authorization, use it and confirm the old token is invalid. If you cannot invalidate it, wait until its displayed expiry before publishing workflow 03. Do not paste either token into chat, shell history, GitHub, Render logs, screenshots, or workflow JSON.
3. Save the fresh token only in the hosted n8n **Restaurant Meta Test API** Bearer Auth credential. Replace the credential's old value if one exists. `Meta__AppSecret` and `Meta__VerifyToken` are separate values; changing the temporary access token does not rotate those.

Meta's **Hello World sent** result tests the outbound `/messages` endpoint; it does not establish that inbound `messages` webhooks are subscribed or reaching the API. Keep the app in Development mode and use only the test number and verified recipient.

1. In Neon, create a **business** database and role separate from the n8n automation database. Run the EF migrations from a trusted workstation using the direct Neon hostname and a migration role. The command is `ASPNETCORE_ENVIRONMENT=Staging .tools/dotnet/dotnet run --project backend/src/Restaurant.API --no-launch-profile -- --migrate` with `ConnectionStrings__DefaultConnection` supplied privately. Confirm `/health/ready` later returns 200. Do not run this against the n8n database or seed test data in Staging.
2. In Render, create a new Blueprint from GitHub `main` using Blueprint Path **`backend/render.yaml`**. This creates `restaurant-api-test` separately from `restaurant-n8n`. Fill the secret fields above; use the **pooled** business Neon connection string and keep `SSL Mode=VerifyFull`. Keep automatic deployment off. Check the service's `/health/live` and `/health/ready` endpoints after deploy. Its public HTTPS URL will be the callback origin.
3. In Meta for Developers, create/select an app with the WhatsApp use case and keep it in **Development** mode. In WhatsApp **API Setup / Getting Started**, select Meta's **test number** and note its **Phone Number ID**, WABA ID, temporary access token and the Graph API version displayed in your setup. In the **To / Manage phone number list** area, add and verify only your private test recipient. The temporary user token expires; when it does, update only the hosted Bearer Auth credential. [Meta's Cloud API reference](https://www.postman.com/meta/whatsapp-business-platform/documentation/wlk6lh4/whatsapp-cloud-api) documents the test send endpoint and token behavior.
4. In App settings → Basic, reveal the **App Secret** and save it only as `Meta__AppSecret` in Render. Generate a distinct random verify token privately, save it as `Meta__VerifyToken`, and keep the same value ready for Meta's callback form. Set the test Phone Number ID and allowlisted recipient in the two Render variables. Redeploy the API after changing variables.
5. In hosted n8n, create a Header Auth credential named **Restaurant Meta Bridge** with header `X-Restaurant-Meta-Bridge-Token` and the exact secret saved as `Automation__MetaBridgeToken`. Create a Bearer Auth credential named **Restaurant Meta Test API** containing only the current temporary Graph access token. Import `automation/workflows/03-meta-test-bridge.json` as a **new** workflow. In `Validate test message`, replace both placeholders with the test Phone Number ID and the one verified recipient. In `Send via Meta test number`, replace `REPLACE_WITH_GRAPH_VERSION` with the supported version displayed by Meta and replace the Phone Number ID placeholder. Bind the two new credentials and bind the **existing** `Restaurant development webhook` credential to `Call existing AI conversation`. Save and publish workflow 03 only after reviewing the test recipient and URL. Do not reimport workflows 01 or 02.
6. In Meta's WhatsApp → Configuration / Webhooks panel, set Callback URL to `https://<your-restaurant-api-test>.onrender.com/api/v1/webhooks/whatsapp` and Verify Token to the exact `Meta__VerifyToken`. Select **Verify and save**, then subscribe the WhatsApp `messages` field. Ensure this app is also subscribed to the **test WABA**; Meta's Cloud API collection documents `POST /{WABA-ID}/subscribed_apps` if the dashboard has not done this. The GET handshake should return the challenge as plain text; bad tokens return 403. Use Meta's dashboard test delivery or send `Hello` from your verified recipient to the **test** number. The signed POST should create one `MetaWebhookEvents` row and, with workflow 03 published, produce one mock AI reply to that same recipient.

The callback URL must point to the **API**, not the n8n bridge. Never paste the App Secret or access token into a screenshot or issue. Use a fresh Meta message ID for each live test; resending the same webhook ID is expected to be deduplicated.

## Acceptance and troubleshooting

Hosted acceptance is still pending: verify the Meta Console challenge, a genuine signed test message, one stored message ID, one AI reply delivered to the allowlisted test recipient, duplicate retry suppression, and a restart retaining the row and n8n credentials. No live WhatsApp messaging or payment processing is enabled.

- **Meta cannot verify callback:** confirm the API Render URL is awake and `/health/ready` passes, callback path is exact, and the verify token values match. GET 403 means mode/token/challenge mismatch.
- **POST 403:** check that `Meta__AppSecret` is the selected app's App Secret; do not use the Graph access token as the signing secret. HMAC covers the original request bytes.
- **POST 400:** inspect the test phone ID, allowlisted sender and payload shape. Do not relax allowlist guards for convenience.
- **Event stays `Pending`:** confirm `Automation__MetaBridgeUrl` and token are configured and restart the API worker. Inspect only event ID/state, not customer text or secrets.
- **`Failed` or `Unknown`:** check n8n execution errors and credential validity. A temporary Graph token can expire; replace it in the Bearer Auth credential. Do not automatically resend ambiguous deliveries.
- **429 or delayed delivery:** respect the provider limit; Render Free sleep can delay requests. This setup is unsuitable for real customer orders.

References: [Meta Cloud API and test send](https://www.postman.com/meta/whatsapp-business-platform/documentation/wlk6lh4/whatsapp-cloud-api), [Meta Webhooks](https://www.postman.com/meta/whatsapp-business-platform/folder/lboq68h/webhooks), [Meta Graph Webhook verification/signatures](https://developers.facebook.com/docs/graph-api/webhooks/getting-started/).
