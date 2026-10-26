# Installation and common failures

Follow README for exact local commands. Run from the monorepo root; shell environment variables are not automatically loaded from `.env` by ASP.NET. Compose uses its project path rules; pass `--env-file .env` explicitly if using a root file. `source scripts/dev-env.sh` enables workspace runtimes and cache locations when available.

| Symptom | Resolution |
| --- | --- |
| NETSDK1045 | Install .NET 10 SDK and confirm `dotnet --version` from root. Do not retarget to .NET 9. |
| Angular refuses Node | Use Node 24.15.0; 24.3.0 is too old for Angular 22. |
| Docker socket unavailable | Start Docker Desktop and use an authorized terminal. |
| Port 5432 busy | Stop the conflicting local database or map another host port and update connection strings. Do not delete unknown containers. |
| Ready returns 503 | Start PostgreSQL, verify connection/role/TLS, apply canonical migrations. |
| Profile returns 404 | Empty database is valid. Seed only Development or create profile through a later authenticated setup use case. |
| Dashboard API unavailable | Start API on 5080; restart Angular after changing proxy config. Hosted builds need public API URL and exact API CORS origin. |
| Neon certificate error | Verify hostname/system trust and VerifyFull. Never disable certificate validation. |
| Testcontainers fails | Confirm Docker is running; tests need a disposable database, never staging/production. |
| Browser binaries absent | Run `npx playwright install chromium` (CI uses `--with-deps`). |
| Render deep link 404 | Publish `dist/restaurant-admin/browser` and configure rewrite to `/index.html`. |
| npm/dotnet downloads time out | Retry with network access; preserve pinned manifests and lockfiles. Do not claim an unexecuted build passed. |

No paid subscription is needed for local development. External free accounts require their owner's setup; free plan quotas and actual availability must be checked before provisioning.
