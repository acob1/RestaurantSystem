# Validation evidence

Execution date: 2026-10-26. This file will record actual local results and distinguish them from external acceptance.

Configuration checks: Docker Compose parsed successfully with a local-only encryption-key placeholder. Render Blueprint passed validation against the official JSON schema from https://render.com/schema/render.yaml.json. Structure assertions verify three services, static output path and routing fallback, main branch/manual deployment, and n8n TLS verification. PostgreSQL 17.6-alpine started and pg_isready reported accepting connections.

Executed and passed: Angular 22.2.3 production build (303.26 kB initial raw bundle), 5 TestBed/Vitest tests across 3 files with V8 coverage, and 7 C# domain tests on .NET SDK 10.0.403. The patched npm install audited 329 packages and reported zero vulnerabilities. Angular coverage is limited to imported/tested files; it is not full application coverage.

The initial npm resolver/native-package problems were resolved by regenerating the lockfile under Node 24.15 and npm 11.13.0. The production build passed outside sandbox worker restrictions. The backend solution and EF tool restored using Microsoft’s official mirror after NuGet connection failures.

Container build attempts did not pass: n8n registry returned HTTP 429, and Microsoft container registry returned EOF/connection errors. Schema validation does not establish a successful image build or hosted deployment. The full backend solution built with zero warnings and zero errors. The generated InitialCatalog migration was applied and the development seed created four dishes. Seven API integration tests passed against a disposable PostgreSQL 17.6 Testcontainer, including migration/model consistency and foreign-key enforcement. The original integration run found test configuration applied too late for minimal-host startup; using an early host setting fixed the fixture, and the rerun passed.

Two Playwright tests passed against the real local API/database at desktop and Pixel 7 viewport sizes, using the installed Chrome channel. HTTP smoke checks returned Healthy and the seeded restaurant/catalog. Desktop and mobile screenshots are under artifacts/screenshots (ignored generated artifacts).

Verified locally: 7 C# domain tests, 7 PostgreSQL API integration tests, 5 Angular component/service tests, 2 Playwright smoke tests. No tests were skipped. Production Angular build passed; Render schema and Compose validation passed. Remote deployment and Docker image builds are not claimed as passed.

Commands: see README. Integration fixtures create disposable PostgreSQL through Testcontainers, apply real migrations, and test the HTTP pipeline, catalog persistence, pagination errors, relational constraints and model/migration consistency. Unit tests cover catalog input and integer prices. Angular tests cover service requests, live status, failures and menu filters. Playwright exercises real API-driven navigation/menu at desktop/mobile viewports.

External acceptance remains pending: Neon verified connection; GitHub repository and hosted CI; Render deployed HTTPS/deep links/cold-start behavior; n8n persistence across redeploy.

For deterministic E2E fixtures, use a new isolated database ending in _test, set ConnectionStrings__DefaultConnection in Development and run the API with --migrate --seed. Testcontainers automatically cleans up its owned container; no destructive test reset targets a Neon environment.

Optional local Chrome fallback (no browser download): `PLAYWRIGHT_CHROME_CHANNEL=1 npm run e2e`. CI installs Playwright Chromium and uses the default channel. The Angular dev server and API used for verification are stopped after the checks; README commands restart them. The local PostgreSQL development volume is retained.

Final checks: backend locked-mode restore passed from cached packages; final Angular production rebuild passed after logo alignment. Fresh 1440px and 390px screenshots and DOM measurements confirmed no horizontal overflow.

## Automation-first checkpoint (2026-10-26)

The n8n image and automation Dockerfile now downloaded/built successfully, superseding the earlier n8n registry failure recorded above. Four static workflow checks and 15 real n8n/PostgreSQL runtime checks passed. The first authenticated webhook is published in the lasting local automation instance and its smoke check passed. Credential encryption and fresh-container/fresh-user-folder persistence were verified under a 512 MiB application limit. See [detailed automation evidence](automation-milestone-01.md). This does not change the earlier API container registry failure or establish a real Neon/Render deployment.

## Actual automation Neon verification (2026-10-26)

Four Neon-specific verification checks passed after securely configuring the real development database: certificate-verified TLS 1.3 and permissions; persisted workflow/credential records; authenticated execution before and after recreating the application container; and the original local instance retained. See automation-milestone-01.md for the exact grouping and measured response times. An initial 15-second request timed out; recreation verification used a 60-second development timeout and measured 10.601 seconds before/30.462 seconds after recreation. This establishes automation database connectivity/persistence, not business-Neon or Render readiness.
