# Validation evidence

Execution date: 2026-10-26. This file will record actual local results and distinguish them from external acceptance.

Configuration checks: Docker Compose parsed successfully with a local-only encryption-key placeholder. Render Blueprint passed validation against the official JSON schema from https://render.com/schema/render.yaml.json. Structure assertions verify three services, static output path and routing fallback, main branch/manual deployment, and n8n TLS verification. PostgreSQL 17.6-alpine started and pg_isready reported accepting connections.

Executed and passed: Angular 22.2.3 production build (303.26 kB initial raw bundle), 5 TestBed/Vitest tests across 3 files with V8 coverage, and 7 C# domain tests on .NET SDK 10.0.403. The patched npm install audited 329 packages and reported zero vulnerabilities. Angular coverage is limited to imported/tested files; it is not full application coverage.

The initial npm resolver/native-package problems were resolved by regenerating the lockfile under Node 24.15 and npm 11.13.0. The production build passed outside sandbox worker restrictions. The backend solution and EF tool restored using Microsoft’s official mirror after NuGet connection failures.

The first container build attempts did not pass: n8n registry returned HTTP 429, and Microsoft container registry returned EOF/connection errors. The n8n build was later fixed and hosted successfully, as recorded below; the API image has not been verified. The full backend solution built with zero warnings and zero errors. The generated InitialCatalog migration was applied and the development seed created four dishes. Seven API integration tests passed against a disposable PostgreSQL 17.6 Testcontainer, including migration/model consistency and foreign-key enforcement. The original integration run found test configuration applied too late for minimal-host startup; using an early host setting fixed the fixture, and the rerun passed.

Two Playwright tests passed against the real local API/database at desktop and Pixel 7 viewport sizes, using the installed Chrome channel. HTTP smoke checks returned Healthy and the seeded restaurant/catalog. Desktop and mobile screenshots are under artifacts/screenshots (ignored generated artifacts).

Verified locally for the foundation: 7 C# domain tests, 7 PostgreSQL API integration tests, 5 Angular component/service tests, 2 Playwright smoke tests. No tests were skipped. Production Angular build passed; Render schema and Compose validation passed. The subsequent n8n image build and hosted checks are recorded below.

Commands: see README. Integration fixtures create disposable PostgreSQL through Testcontainers, apply real migrations, and test the HTTP pipeline, catalog persistence, pagination errors, relational constraints and model/migration consistency. Unit tests cover catalog input and integer prices. Angular tests cover service requests, live status, failures and menu filters. Playwright exercises real API-driven navigation/menu at desktop/mobile viewports.

External acceptance still pending for the business application: business-Neon connection, hosted CI, API/dashboard hosting and deep links, and cold-start behavior under webhook load. Automation Neon connectivity and n8n persistence across an observed Render restart are verified below.

For deterministic E2E fixtures, use a new isolated database ending in _test, set ConnectionStrings__DefaultConnection in Development and run the API with --migrate --seed. Testcontainers automatically cleans up its owned container; no destructive test reset targets a Neon environment.

Optional local Chrome fallback (no browser download): `PLAYWRIGHT_CHROME_CHANNEL=1 npm run e2e`. CI installs Playwright Chromium and uses the default channel. The Angular dev server and API used for verification are stopped after the checks; README commands restart them. The local PostgreSQL development volume is retained.

Final checks: backend locked-mode restore passed from cached packages; final Angular production rebuild passed after logo alignment. Fresh 1440px and 390px screenshots and DOM measurements confirmed no horizontal overflow.

## Automation-first checkpoint (2026-10-26)

The n8n image and automation Dockerfile downloaded/built successfully, superseding the earlier n8n registry failure recorded above. Four static workflow checks and 15 real n8n/PostgreSQL runtime checks passed. The first authenticated webhook was published in the lasting local automation instance and its smoke check passed. Credential encryption and fresh-container/fresh-user-folder persistence were verified under a 512 MiB application limit. See [detailed automation evidence](automation-milestone-01.md). This does not change the earlier API container registry failure.

## Actual automation Neon verification (2026-10-26)

Four Neon-specific verification checks passed after securely configuring the real development database: certificate-verified TLS 1.3 and permissions; persisted workflow/credential records; authenticated execution before and after recreating the application container; and the original local instance retained. See automation-milestone-01.md for the exact grouping and measured response times. An initial 15-second request timed out; recreation verification used a 60-second development timeout and measured 10.601 seconds before/30.462 seconds after recreation. This establishes automation database connectivity/persistence, not business-Neon readiness.

## Hosted n8n Render verification (2026-10-26)

Render built the pinned n8n 2.44.7 image from the official Docker Hub mirror and marked the service Live. Hosted readiness returned HTTP 200 after startup. The published development workflow returned its expected response to an authenticated mock request; missing authentication returned 403 and invalid input returned 422. The hosted editor showed the workflow Published. A later Render deployment registered the JavaScript task runner. The user explicitly restarted the service, and the remote readiness plus authenticated webhook script passed again afterward. These checks demonstrate this workflow and its protected credential remained usable across the observed restart. They do not establish continuous availability on Render Free or any real ordering/payment capability. The local rebuild attempted for the runner timeout setting was unavailable because Docker Desktop's daemon was down; four static checks passed for that revision.

## Rotated Neon credential and milestone 02 (2026-10-27)

After the user rotated the automation database password, a read-only direct `psql` check passed with full certificate verification and required channel binding. It confirmed the expected role/database, schema permissions, and existing workflow and credential rows. The hosted readiness and authenticated development webhook also passed. A scan of tracked files and committed snapshots found no Neon-style password tokens or Neon connection URLs with embedded passwords; `automation/.env.neon` remains ignored and untracked. This is a pattern-based scan, not proof about arbitrary unknown password formats.

The separate Groq mock-conversation workflow passed **13 code/export tests** and **14 disposable n8n/PostgreSQL runtime checks** against a local fake Groq API. The final JSON imported/published in n8n 2.44.7, worked beside workflow 01, required Header Auth, returned greeting/menu/mock-order/fallback responses, handled rate limits/timeouts/malformed replies safely, rejected invented SKUs, and stored its fake Bearer Auth credential encrypted. See [milestone 02](automation-milestone-02.md). The hosted workflow has not been imported or tested with the real Groq API yet.
