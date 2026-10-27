# Incremental automation workflows

`01-development-inbox.json` remains the published first checkpoint. It requires Header Auth, validates simulated messages, and acknowledges them without creating orders. Do not edit or reimport it on the hosted instance.

`02-ai-conversation.json` is a separate, inactive-on-import development workflow. It uses the existing Header Auth credential plus a new Bearer Auth credential for Groq. Its prompt and mock menu are under `../ai/`; setup and actual tests are recorded in `../../docs/automation-milestone-02.md`.

Do not generate or activate the original fourteen workflows in bulk. After hosted Groq acceptance, continue with verified Meta test ingress. The backend, real menus, payments and dashboard integrations follow later.
