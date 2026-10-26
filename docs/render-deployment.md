# Render deployment

The Blueprint is `infrastructure/render/render.yaml`; select this non-root path when creating it. It defines a static Angular site and two free Docker services. Auto deployment is off. Deploy from protected `main` only after CI succeeds and a maintainer explicitly starts deployment; configure branch protection to require all foundation checks. No deployment secrets are used in pull-request workflows.

1. Create a GitHub repository and push the monorepo. These files do not create a remote repository.
2. Create separate Neon business and automation databases/roles. Set the API Npgsql connection string with `SSL Mode=VerifyFull`. Set n8n PostgreSQL host/database/user/password independently, plus verified SSL and pool size 2.
3. Create services from the Blueprint. Fill API CORS origin with the exact static-site HTTPS origin and `PUBLIC_API_BASE_URL` with the API HTTPS origin. CORS has no wildcard or credential authorization.
4. Run business migrations once from a trusted workstation/CI administration context using direct Neon credentials: `ASPNETCORE_ENVIRONMENT=Staging dotnet run --project backend/src/Restaurant.API --no-launch-profile -- --migrate`. Do not seed staging with fixtures.
5. Set n8n's stable `N8N_ENCRYPTION_KEY`, public `N8N_HOST` (hostname only), `WEBHOOK_URL`, and `N8N_EDITOR_BASE_URL` (HTTPS URLs). Save/recover the encryption key in a secret manager; never regenerate it during redeploy. Create the n8n owner account promptly. No n8n application credential goes in Angular.
6. Deploy and verify `/health/live`, `/health/ready`, `/api/v1/system/status`, static deep links `/menu` and `/setup`, and n8n `/healthz`. Confirm a database restart is reported as not ready and recovers.
7. Redeploy n8n and confirm workflows/credentials remain decryptable. Workflows are not part of Phase 1. Do not configure Meta webhooks yet.

The API responds on `0.0.0.0:$PORT`; n8n port is pinned to Render's default 10000. Angular uses Node 24.15, `npm ci`, the application builder, and publishes `dist/restaurant-admin/browser` with SPA fallback. It has no Node hosting process. `config.json` is public and holds only the API URL.

## Validation levels

Local: parse/validate Blueprint against Render's JSON schema; validate Compose; build both Dockerfiles; smoke-test API container with PostgreSQL. Remote: Render Blueprint validation API/CLI and an actual account deployment, HTTPS, origins, sleeping/restart behavior. Passing local validation never means a hosted deployment was tested. Record actual results in testing.md.

Free instances sleep and have ephemeral disks and memory limits. No receipt or application file may be stored locally. An inbox/outbox can only preserve events already committed to Neon. Later cold-start tests must deliver a signed webhook, stop n8n after commit, restart/redeploy, replay by event ID, and prove one financial effect and one deduplicated notification. Also test a request that times out before delivery and Meta retry behavior. None of that exists in Phase 1. Always-on API and automation plus persistent processing/reconciliation are required before real orders.

References: [Blueprint schema and fields](https://render.com/docs/blueprint-spec), [n8n PostgreSQL environment](https://docs.n8n.io/hosting/configuration/environment-variables/database/), [n8n execution retention](https://docs.n8n.io/hosting/configuration/environment-variables/executions/).
