# Phase 1 acceptance

Objective: a test-only foundation with independently deployable API/dashboard/automation infrastructure. Architecture: Domain → no dependencies; Application → Domain; Infrastructure → Application; API composes everything. See architecture.md for system and hosting diagrams.

Source: complete initial API, catalog model/query DTOs, EF configuration/migration, Angular overview/menu/setup, Dockerfiles, Compose, Render Blueprint, xUnit/WebApplicationFactory/Testcontainers, TestBed/Vitest, Playwright and GitHub Actions. Database changes are limited to the initial catalog/profile schema. `.env.example` documents public versus secret settings; staging/production remain Test.

Commands and expected outcomes are in README and testing.md. Deployment steps and local-versus-remote verification boundaries are in render-deployment.md. Failure solutions are in installation.md.

| Acceptance criterion | Evidence required |
| --- | --- |
| API runs locally | process starts with .NET 10; live check returns 200 |
| Database works | migrations applied; readiness query 200 |
| Neon specifically works | account-configured verified TLS connection and readiness query; local PostgreSQL does not satisfy this |
| Angular communicates with API | real API-driven health/profile/menu and Playwright smoke |
| Angular component test passes | executed TestBed/Vitest result |
| ASP.NET integration test passes | executed WebApplicationFactory + real PostgreSQL result |
| Deployment config tested locally | schema validation, Compose validation, API/n8n image builds and smoke test |
| Render deployment tested remotely | actual deployment/HTTPS/deep-link/sleep checks in an authorized account |
| GitHub monorepo exists | remote URL and successful CI run; source files alone do not satisfy this |

The first working milestone is not complete until Neon and hosted Render checks are also verified. Do not begin financial or WhatsApp integration by treating these account-dependent gates as satisfied.

## Recorded result on 2026-10-26

Local API, PostgreSQL migration/readiness, Angular-to-API communication, both test stacks, desktop/mobile browser smoke, Compose parsing and Render JSON schema validation passed. Neon connectivity, GitHub remote/CI and hosted Render checks remain pending account access. API/n8n Docker build attempts were blocked by registry connection errors/HTTP 429. Phase 1 remains externally incomplete until those checks pass.
