# Automation milestone 01 — execution evidence

Date: 2026-10-26. Active scope: start n8n independently and test one authenticated development webhook. Angular, ASP.NET Core, business migrations and the existing three-service deployment configuration are retained.

## Previous foundation results

Recorded from the executed Phase 1 checks: 7 C# domain tests, 7 PostgreSQL API integration tests, 5 Angular tests and 2 Playwright desktop/mobile smoke tests passed. Backend and Angular production builds passed. These suites were not rebuilt or rerun for the automation-only change. GitHub main was pushed to https://github.com/acob1/RestaurantSystem; hosted CI results are not asserted here.

## Current verified results

- Official n8n image downloaded and actual container version confirmed as **2.44.7**.
- The automation Dockerfile built successfully.
- Four workflow static/contract checks passed using Node's built-in test runner.
- Standalone Compose validation passed. Original three-service and new n8n-only Render Blueprints passed validation against the official Render JSON schema.
- Local n8n is running on http://localhost:5678 with a dedicated local PostgreSQL database and separate volumes.
- The first Header Auth credential and workflow were imported and published locally. The local authenticated webhook smoke check passed.
- **All 15 disposable runtime integration checks passed**, including valid/repeated requests, missing/invalid authentication, invalid text/IDs, prompt-as-data handling, confirmed byte-limit rejection, recovery afterward, PostgreSQL credential encryption, and workflow/credential survival after replacing both the application container and user folder.
- The enforced application memory limit was **512 MiB**; the final measured snapshot was **469.5 MiB / 512 MiB**. This is a snapshot under this small workload, not a production capacity claim or a hosted Render test.
- Repeated local setup/bootstrap retained the existing encryption key, webhook token, workflow and credential, and the subsequent authenticated local smoke check passed. Startup was also executed with deliberately conflicting shell database/key values; the helper correctly preferred the private local configuration, recreated n8n successfully, and the webhook still passed.

The runtime tests found two harness issues and one upstream behavior. CLI imports were moved into stopped-server, one-shot containers after a combined server/CLI process exceeded 512 MiB. The harness now rediscovers Docker's randomly assigned port after restart. The pinned n8n runtime rejects bodies over the configured limit but converts PayloadTooLargeError to HTTP 500 rather than 413; the test checks the specific error in server logs, plus a subsequent successful normal request.

Runtime evidence is written without credentials to ignored `artifacts/automation/milestone-01.json`. Durable local settings remain in ignored mode-0600 `automation/.env.local`; bootstrap reruns retain existing workflow/credential records. No lasting owner password was assigned: complete owner setup in the local editor.

## Remaining external checks

Neon was subsequently configured through the Console and private local settings. Real automation Neon authentication, TLS 1.3 certificate verification, schema permissions, workflow/credential persistence, and container-recreation verification passed (see the checkpoint below). The Render Blueprint was connected to GitHub, but its first build failed before n8n started because `docker.n8n.io` returned HTTP 429 while resolving the pinned image. The Dockerfile was then switched to the official Docker Hub mirror with the same verified image digest; a local build with `--pull` passed. A successful hosted Render deployment/redeploy test is still pending. The business application has not yet been connected to Neon. Local PostgreSQL and a 512 MiB container are not proof of hosted behavior. No paid service or real ordering/payment was enabled.

Groq and Meta developer credentials were absent. No live Groq call, WhatsApp verification/message exchange, mock-menu AI agent, real cart/order/payment flow, or additional workflow has been represented as complete. Verify the current checkpoint first, then proceed one integration at a time.

## Repeat

```sh
export PATH="$PWD/.tools/node/bin:$PATH"
node --test automation/tests/workflow.test.mjs
node automation/scripts/integration-test.mjs
node automation/scripts/check-local.mjs
```

The integration run uses only uniquely named disposable local resources and removes them after completion. It never resets the lasting local/Neon databases. The normal local automation stack remains available for the user's next setup step.

## Real Neon checkpoint — passed

On 2026-10-26, the saved private credentials connected as `restaurant_automation` to `restaurant_automation_dev` through a certificate-verified TLS 1.3 socket. Schema USAGE/CREATE checks passed. n8n applied its own migrations; no business migration or test reset was run against Neon. The first workflow and protected credential were added while preserving the original local instance.

Four further checks passed: authenticated webhook before recreation; the same response/decrypted credential after recreating the Neon-backed container; original local container unchanged; and verified Neon TLS/permissions plus the two persisted records. Evidence: ignored `artifacts/automation/neon-verification.json`. The private file remains ignored and the encryption key is retained.

Observed limitation: the first 15-second webhook smoke request timed out. A later authenticated probe returned HTTP 200 in 8.8 seconds. The recorded recreation check took 10.601 seconds before recreation and 30.462 seconds afterward, using an explicit 60-second development timeout. These are functional/persistence checks, not a production response-time acceptance. No TLS verification was disabled to obtain them. The original local n8n workspace remains on 5678; the Neon-backed development workspace is on 5679.

Owner setup completed in the Neon-backed editor. A later settings error toast coincided with one logged database ping timeout; the connection recovered, `/rest/settings` returned HTTP 200, and a new authenticated development webhook smoke test passed. The optional n8n Assistant model setup was not configured. For Render, retain the same encryption key and stop the 5679 main process before starting another n8n main process against this database. The owner, workflow and credential should carry over from Neon. Render hosting and its latency remain untested.
