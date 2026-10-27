# Incremental automation workflows

`01-development-inbox.json` remains the published first checkpoint. It requires Header Auth, validates simulated messages, and acknowledges them without creating orders. Do not edit or reimport it on the hosted instance.

`02-ai-conversation.json` is a separate development workflow, now published on hosted n8n. The export remains inactive on import. It uses the existing Header Auth credential plus a Bearer Auth credential for Groq. Its prompt and mock menu are under `../ai/`; setup and actual tests are recorded in `../../docs/automation-milestone-02.md`.

`03-meta-test-bridge.json` is a separate, inactive-on-import **test-only** workflow. It accepts only authenticated backend dispatch, calls workflow 02, and can send a reply through Meta's test number to one configured recipient. Replace its phone/recipient/Graph-version placeholders and bind dedicated credentials before publishing. Its local simulated checks and hosted setup are in `../../docs/automation-milestone-03.md`. Workflows 01 and 02 must not be reimported or changed.

Do not generate or activate the original fourteen workflows in bulk. Hosted Meta test acceptance is the next gate. The backend business operations, real menus, payments and dashboard integrations follow later.
