# n8n foundation

Image pinned to Community Edition 2.44.7, separate PostgreSQL persistence, stable encryption key, TLS verification for Neon, pool 2, proxy hops 1 on Render, bounded execution retention and no stored success/error execution payloads. Workflows and credentials live in PostgreSQL; local optional volume stores additional local state. Do not rely on Render filesystem state. Confirm supported environment variables against the pinned runtime before deployment.

No workflow runs in Phase 1. Phase 3 must add the fourteen real importable JSON exports listed in the build brief, with input/output contracts, idempotency keys, HTTP credentials stored as protected n8n credentials, backend-owned action allowlists, error routes, fixtures, and import/execution tests. Never put a shared secret directly in exported JSON. AI and n8n must have no finance approval endpoint permission.

Recovery will use backend leases and pending events, not a timer on a sleeping service as the sole recovery mechanism. A trusted external scheduler/operator must trigger replay. Production requires persistent reliable processing and always-on infrastructure.

References: [database configuration](https://docs.n8n.io/hosting/configuration/environment-variables/database/), [execution retention](https://docs.n8n.io/hosting/configuration/environment-variables/executions/).
