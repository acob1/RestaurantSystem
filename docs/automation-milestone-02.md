# Automation milestone 02 — Groq mock conversation

Final local verification: 2026-10-27. Scope: a **separate** development workflow for greeting, mock-menu and simple-order conversations. Workflow 01, ASP.NET Core, Angular and business data were not changed. No WhatsApp, Paystack, payment or real order operation is connected.

## Source and contract

- Importable n8n export: [`automation/workflows/02-ai-conversation.json`](../automation/workflows/02-ai-conversation.json).
- Reviewable [system prompt](../automation/ai/system-prompt.txt) and [mock menu](../automation/ai/mock-menu.json). Prices are integer pesewas in GHS; they are demo data.
- POST `/webhook/restaurant-dev/ai-conversation` with the same Header Auth credential as workflow 01. JSON input: `messageId`, `customerId` prefixed `mock-`, and `text` (1–1000 characters). The workflow is inactive on import.
- Groq Chat Completions uses `openai/gpt-oss-20b` with a strict JSON schema to extract one intent (`greeting`, `menu`, `order`, `fallback`) and optional SKU/quantity pairs. n8n calls the fixed HTTPS Groq URL using an encrypted **Bearer Auth** credential named `Restaurant Groq API`.
- A Code node validates the model result, checks SKUs and quantities against the fixed menu, and computes any mock total from integer prices. The model cannot set prices. Replies are development-only and explicitly say no real order or charge occurred. Provider errors, rate limits, timeouts and malformed responses return a safe 503 message.
- No conversation history is persisted in this milestone; each request is handled independently. No external tool access is offered to the model. n8n execution bodies are not saved.

## Local checks actually run

- `node --test automation/tests/*.test.mjs`: **13 passed** against the final export. Tests execute the actual embedded Code-node JavaScript in a VM, check the exported graph and credentials, validation, greetings, menu, minor-unit totals, invented SKU rejection, provider failures and prompt-as-data handling.
- `node automation/scripts/ai-integration-test.mjs`: **14 passed** against the final export in disposable n8n 2.44.7, PostgreSQL 17.6 and a local fake Groq API. The JSON export imported and published. Both workflow 01 and 02 ran in the same disposable n8n instance. The AI webhook required authentication; greetings, menu, order, fallback, HTTP 429, malformed response, timeout and invented SKU handling passed. The fake Groq Bearer Auth credential was encrypted in PostgreSQL. The script removed its disposable containers, network and volume afterward.

The disposable runtime test substitutes only the Groq URL with the local fake provider. It does not call Groq or import workflow 02 into the lasting Neon/Render instance. A live Groq test requires the user's key in the hosted n8n credential UI; no key is stored in source or requested in chat.

## Hosted activation and acceptance

1. In [Groq Console](https://console.groq.com/keys), create a free-tier API key. Keep it private.
2. In the hosted n8n editor, create a credential of type **Bearer Auth**, named `Restaurant Groq API`. Paste the Groq API key as the **Bearer Token** only; n8n supplies the `Bearer` prefix. Do not add the key to the workflow JSON, GitHub, Render variables, or Angular.
3. In hosted n8n, create a new workflow and choose **Import from File**. Select `automation/workflows/02-ai-conversation.json`. Do not reimport or edit workflow 01.
4. Open **AI development conversation** and select the existing `Restaurant development webhook` Header Auth credential. Open **Classify with Groq** and select `Restaurant Groq API` Bearer Auth. Imported credential IDs are placeholders; explicitly reselect both credentials in this instance. Save and publish workflow 02.
5. From the repository root, run the hosted check below. It reads the development webhook token from the ignored private file without printing it. It sends simulated greeting, menu and order requests; these calls consume Groq free-tier allowance.

```sh
N8N_BASE_URL=https://restaurant-n8n-5i1u.onrender.com \
  .tools/node/bin/node automation/scripts/check-ai-remote.mjs --env-file automation/.env.neon
```

Expected result: `Hosted original webhook, AI authentication, Groq greeting, menu and mock-order checks passed.` Do not mark live Groq acceptance complete until this actually runs and passes. If the hosted workflow returns 404, confirm it was published. If it returns 500 or 503, reselect the Bearer Auth credential and inspect n8n's execution error without copying secret values. If Groq returns 429, check account limits and retry later; do not add paid billing solely for this development milestone.

## Reproduce local tests

```sh
.tools/node/bin/node automation/scripts/build-ai-workflow.mjs
.tools/node/bin/node --test automation/tests/*.test.mjs
.tools/node/bin/node automation/scripts/ai-integration-test.mjs
```

The last command requires Docker Desktop and pulls the pinned n8n and PostgreSQL images if absent. It creates and deletes only uniquely named disposable test resources, never the dedicated Neon database.

Provider references: [Groq Chat Completions](https://console.groq.com/docs/api-reference), [Groq structured outputs](https://console.groq.com/docs/structured-outputs), [Groq rate limits](https://console.groq.com/docs/rate-limits), [n8n HTTP Request](https://docs.n8n.io/integrations/builtin/core-nodes/n8n-nodes-base.httprequest/), [n8n HTTP credentials](https://docs.n8n.io/integrations/builtin/credentials/httprequest/).
