# Incremental automation workflows

Only `01-development-inbox.json` is shipped for the first automation checkpoint. It is inactive on import, requires a Header Auth credential, validates simulated messages, and acknowledges them without creating orders. See ../README.md for import, publish, fixtures and real runtime tests.

Do not generate or activate the original fourteen workflows in bulk. Extend one tested checkpoint at a time: actual Neon/Render persistence, authenticated webhook, Groq, verified Meta test ingress, then a mock-menu restaurant agent. The backend, real menus, payments and dashboard integrations follow later.
