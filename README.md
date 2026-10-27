# Accra Kitchen · WhatsApp restaurant platform

Phase 1 implementation of the attached build brief: .NET 10 Clean Architecture API, Angular 22 administration workspace, PostgreSQL migrations, isolated tests, and Render deployment configuration. All payments remain in test mode. This is a development foundation; it cannot accept orders or verify payments yet.

Foundation verification: **7 domain + 7 PostgreSQL API + 5 Angular + 2 Playwright tests passed**, with successful backend/Angular builds and migration. Render schema and Compose validation passed. The pinned n8n image built and n8n is live on Render Free with dedicated Neon persistence; the published development webhook passed authenticated checks before and after a Render restart. The API image build previously encountered registry errors. Business-Neon, API/dashboard hosting, and hosted CI checks remain pending. See [execution evidence](docs/testing.md).

## Current priority: n8n automation

The development order focuses on n8n before expanding the backend/dashboard. Existing application work is retained. The first automation milestone has a published authenticated development webhook on [Render](https://restaurant-n8n-5i1u.onrender.com), dedicated Neon persistence, 4 passing initial static checks and 15 passing disposable runtime checks. The rotated Neon password was verified through a direct certificate-checked connection and the hosted webhook. A separate [Groq mock-conversation workflow](docs/automation-milestone-02.md) has passed local tests; hosted credential binding and live Groq tests remain. The original local workspace uses http://localhost:5678; its Neon-backed counterpart on 5679 was stopped before Render started. The original application commands below still apply.

## Run locally

Requirements: .NET SDK 10.0.100 or newer 10.0 feature band, Node **24.15.0**, npm, Docker. `global.json` selects .NET 10. Workspace runtimes, if present, can be enabled with `source scripts/dev-env.sh`.

```sh
# From repository root. Compose requires a stable local encryption key even without n8n.
export N8N_ENCRYPTION_KEY=local-development-key-never-use-hosted
docker compose -f infrastructure/docker/docker-compose.yml up -d business-db
dotnet restore backend/Restaurant.slnx
dotnet run --project backend/src/Restaurant.API -- --migrate --seed
dotnet run --project backend/src/Restaurant.API
```

In another terminal:

```sh
cd frontend/restaurant-admin
npm ci
npm start
```

Open http://localhost:4200. The API listens on http://localhost:5080; Angular proxies `/api` and `/health`. `/health/live` checks the process; `/health/ready` queries the migrated database. Development OpenAPI is `/openapi/v1.json`.

The seed creates an example Ghanaian menu with test prices. No account or third-party credential is needed for local PostgreSQL development. To use Neon, follow [database setup](docs/database.md) and override `ConnectionStrings__DefaultConnection` in the API process. Never put it in Angular.

## Test

```sh
dotnet test backend/Restaurant.slnx
cd frontend/restaurant-admin
npm test
npm run test:coverage
npm run build
npx playwright install chromium
# Requires seeded API running on port 5080; Playwright starts Angular.
npm run e2e
```

Integration tests use PostgreSQL Testcontainers and require Docker. They do not use EF InMemory. An optional `TEST_DATABASE_CONNECTION` must name an isolated database ending in `_test`. See [testing](docs/testing.md) for scope and actual execution evidence.

## Containers

```sh
export N8N_ENCRYPTION_KEY=local-development-key-never-use-hosted
docker compose -f infrastructure/docker/docker-compose.yml build api
docker compose -f infrastructure/docker/docker-compose.yml run --rm api --migrate --seed
docker compose -f infrastructure/docker/docker-compose.yml up -d api
# Optional n8n infrastructure; workflows arrive after the milestone gate.
docker compose -f infrastructure/docker/docker-compose.yml --profile automation up -d n8n
```

No automatic startup migrations run in the web server. Run the one-shot migration command first. Avoid concurrent migration executions. Do not use development seeds on staging/production.

## Repository

```text
backend/
  Restaurant.slnx
  src/Restaurant.Domain/          domain entities and rules
  src/Restaurant.Application/     DTOs and query contracts
  src/Restaurant.Infrastructure/  EF Core, PostgreSQL, canonical Migrations/
  src/Restaurant.API/             endpoints, health, errors, rate limits, OpenAPI
  tests/Restaurant.UnitTests/
  tests/Restaurant.IntegrationTests/
  Dockerfile
frontend/restaurant-admin/        standalone Angular features, Vitest, Playwright
automation/                      pinned n8n Dockerfile; Phase 3 workflow location
infrastructure/docker/           local Compose
infrastructure/render/           three-service Render Blueprint
database/                        seed instructions and operational scripts
tests/                           cross-service fixtures
docs/                            architecture, setup, security, phase acceptance
.github/workflows/ci.yml          build, unit, PostgreSQL integration, E2E, images
```

[Architecture](docs/architecture.md) · [Phase 1 acceptance](docs/phase-1.md) · [Installation](docs/installation.md) · [Render](docs/render-deployment.md) · [Dependency compatibility](docs/dependencies.md) · [Implementation status](docs/implementation-status.md)

The GitHub repository and dedicated automation Neon/Render deployment are configured. Business-Neon, Meta developer setup, Cloudinary account, and API/dashboard hosting require later account configuration. The local directory is the monorepo root; a redundant nested `restaurant-whatsapp-ai` directory is unnecessary.
