# Community Edition. Workflows are introduced in Phase 3, not silently enabled here.
FROM docker.n8n.io/n8nio/n8n:2.44.7
ENV N8N_DIAGNOSTICS_ENABLED=false \
    N8N_PERSONALIZATION_ENABLED=false \
    EXECUTIONS_DATA_PRUNE=true \
    EXECUTIONS_DATA_MAX_AGE=48 \
    EXECUTIONS_DATA_PRUNE_MAX_COUNT=1000 \
    EXECUTIONS_DATA_SAVE_ON_SUCCESS=none \
    EXECUTIONS_DATA_SAVE_ON_ERROR=none
