# Architecture and trust boundaries

Phase 1 establishes the application and deployment boundaries. Dashed integrations and financial processing below are target architecture for later phases; only the catalog/readiness path is implemented now.

```mermaid
flowchart TD
  Customer[WhatsApp customer] --> Meta[Meta WhatsApp Cloud API]
  Meta -. signed raw-body webhook .-> API[ASP.NET Core 10 API]
  Staff[Restaurant staff] --> Admin[Angular 22 static dashboard]
  Admin -->|HTTPS REST /api/v1| API
  API --> Application[Application use cases and DTOs]
  Application --> Domain[Pure C# domain rules]
  API --> Infra[Infrastructure adapters]
  Infra --> Business[(Neon business PostgreSQL)]
  Infra -. authenticated receipt uploads .-> Cloudinary[Cloudinary]
  Infra -. test initialization and verification .-> Paystack[Paystack Test API]
  Business -. durable inbox and outbox .-> Dispatcher[Leased event dispatcher]
  Dispatcher -. authenticated event .-> N8N[n8n Community Edition]
  N8N -. restricted tool contracts .-> API
  N8N -. conversation intent only .-> Groq[Groq or local Ollama]
  N8N --> Automation[(Separate Neon automation PostgreSQL)]
  Infra -. trusted outbound messages .-> Meta
```

Domain has no external packages. Application references Domain; Infrastructure references Application and Domain; API composes Infrastructure and Application. EF entities never cross API boundaries. Angular uses typed DTOs and HTTP services, lazy routes, signals for local state, RxJS for polling, and Reactive Forms for menu filtering.

The server owns all future prices, cart mutations, inventory, checkout totals, transitions, financial decisions, and authorization. A screenshot, OCR candidate, redirect, AI tool call, or n8n result cannot mark a payment verified.

## Hosting topology

```mermaid
flowchart LR
  Browser --> Static[Render static Angular site]
  Browser --> API[Render Docker API]
  API -->|TLS VerifyFull and pool max 10| NB[(Neon business database)]
  API -. public menu and authenticated receipts .-> C[Cloudinary]
  API -. future durable dispatch .-> N[Render Docker n8n]
  N -->|TLS verified and pool 2| NA[(Neon automation database)]
```

API binds `0.0.0.0:$PORT` on Render; the Angular output is `dist/restaurant-admin/browser`. n8n uses PostgreSQL persistence and a stable encryption key, with no durable dependency on Render's ephemeral filesystem. Local automation uses a separate database container and volume. Render TLS terminates at the platform edge; forwarded-header trust must be configured explicitly before secure authentication/webhook URL generation in Phase 2/3.

Free services can sleep before a webhook ever reaches the application. Durable ingestion can recover events **after** a successful database commit; it cannot recover a webhook never delivered. Meta retries, reconciliation, replay, and always-on hosting are necessary. Phase 1 does not install a continuously running dispatcher or claim cold-start recovery has been tested.

## Angular feature boundaries

`core/services`: HTTP and public runtime config. `features/dashboard`: health and restaurant summary. `features/menu`: paginated read-only catalog and page-local search. `features/settings`: setup guidance. `shared` and the remaining feature folders reserve the requested structure; they contain no fake implementation. The root shell supplies responsive navigation. Authentication, guards, interceptors, staff navigation, and write forms begin in Phase 2/5/7; no protected operations are exposed before then.

## Storage boundaries

Public menu photos will use ordinary Cloudinary delivery. Sensitive evidence will use `authenticated` delivery, with identifiers and resource/delivery metadata in Neon. Short-lived access will be minted only by an authorized staff endpoint. No receipt URL or file is persisted in Angular or the web-service filesystem. See [Cloudinary plan](cloudinary.md).
