# MASTER BUILD PROMPT
## WhatsApp AI Restaurant Ordering System
### Angular 22 + ASP.NET Core 10 + n8n + Neon + Cloudinary + Render

## 1. YOUR ROLE

Act as a Principal Software Architect, Senior C# ASP.NET Core Engineer, Angular Frontend Engineer, n8n Automation Specialist, PostgreSQL Database Architect, DevOps Engineer, Payment Integration Specialist, and Software Test Engineer.

Your task is to develop a fully functional, production-oriented WhatsApp AI Restaurant Ordering and Management System.

The application must be modular, secure, scalable, maintainable, and thoroughly tested.

This is not a chatbot demonstration. Build a real restaurant ordering platform with a complete backend, database, automation workflows, frontend administration dashboard, customer management, and payment verification system.

IMPORTANT DEVELOPMENT RULE:

The system must initially operate using free tiers, development accounts, test credentials, and simulated payments.

Do not require paid subscriptions during initial development.

Use Render Free Tier for hosting, Neon PostgreSQL for the database, Cloudinary for image storage, and Paystack Test Mode.

The architecture must support future migration to reliable production infrastructure without requiring a complete rewrite.

Do not claim that a free-tier deployment is suitable for accepting real customer orders.

---

# 2. MANDATORY TECHNOLOGY STACK

## Frontend

- Angular 22.
- TypeScript.
- Angular standalone components.
- Angular Router.
- Angular HttpClient.
- Angular Signals for local reactive state.
- RxJS for asynchronous streams.
- Reactive Forms.
- Tailwind CSS.
- Angular route guards.
- Angular HTTP interceptors.
- Responsive mobile-first UI.
- Angular TestBed and Vitest for unit/component testing.
- Playwright for end-to-end testing.

Use Angular's supported modern project structure and built-in application builder.

Do not use React, Next.js, Vue, or another frontend framework.

## Backend

- C#.
- ASP.NET Core 10 Web API.
- .NET 10 LTS.
- Entity Framework Core 10.
- Npgsql PostgreSQL provider.
- Clean Architecture.
- RESTful API endpoints.
- Dependency Injection.
- FluentValidation or equivalent explicit request validation.
- ASP.NET Core authentication and authorization.
- Role-based and policy-based access control.
- OpenAPI documentation.
- Structured logging with Serilog or the built-in logging system.
- xUnit for unit and integration testing.
- Microsoft.AspNetCore.Mvc.Testing with WebApplicationFactory.
- PostgreSQL Testcontainers for isolated integration testing when Docker is available.

Do not use Node.js, Express, NestJS, or Python as the primary backend.

Node.js may be used only as the tooling runtime for Angular or n8n.

## Workflow Automation

- Self-hosted n8n Community Edition.
- n8n hosted on Render Free Web Service.
- Separate Neon PostgreSQL database for n8n persistence.
- Version-pinned n8n Docker deployment.
- Groq AI API with free developer limits.
- Optional local Ollama for offline development.

## Database

- Neon PostgreSQL Free.
- Entity Framework Core migrations for business data.
- PostgreSQL transactions and constraints.
- Database connection pooling.
- Separate application and automation databases.
- Development and production environment isolation.

## Storage

- Cloudinary Free.
- Official Cloudinary .NET SDK.
- Public menu images.
- Authenticated payment receipt storage.
- Signed or otherwise securely authorized receipt access.
- Cloudinary asset metadata stored in Neon.

## Hosting

- Angular dashboard: Render Static Site.
- ASP.NET Core API: Render Free Web Service using Docker.
- n8n: Render Free Web Service using Docker.
- PostgreSQL: Neon Free.
- Images: Cloudinary Free.

## Payment Services

- Paystack Test Mode.
- Simulated Mobile Money transfers.
- Simulated bank transfers.
- Manual payment evidence and review.

## Core Business Configuration

- Initial market: Ghana.
- Currency: GHS.
- Primary communication channel: WhatsApp.
- Default language: English.
- Order fulfillment: Pickup and delivery.

Use compatible, stable, explicitly pinned package versions and document their compatibility.

---

# 3. SYSTEM ARCHITECTURE

Use Clean Architecture for the ASP.NET Core backend.

Organize backend responsibilities into:

### Domain Layer

Contains:

- Business entities.
- Value objects.
- Enumerations.
- Domain rules.
- Domain events.
- Payment state transitions.
- Order state transitions.
- Domain exceptions.

The Domain layer must not depend on Entity Framework Core, n8n, Cloudinary, or Paystack.

### Application Layer

Contains:

- Business use cases.
- Commands and queries.
- Request/response contracts.
- Application validation.
- Service interfaces.
- Authorization requirements.
- Checkout orchestration.
- Payment verification use cases.
- Inventory and cart logic.

Avoid unnecessary abstractions that make the implementation difficult to maintain.

### Infrastructure Layer

Contains:

- Entity Framework Core.
- Neon database access.
- Cloudinary integration.
- Paystack integration.
- Meta WhatsApp integration.
- AI provider integration.
- Notification delivery.
- Repository implementations where useful.
- External service adapters.
- Durable event handling.

### API Layer

Contains:

- REST API controllers or endpoint modules.
- Request validation.
- Authentication.
- Authorization.
- Webhook endpoints.
- API versioning.
- OpenAPI.
- Exception handling.
- Health checks.
- Rate limiting.

Do not expose EF Core entities directly as public API contracts.

Use strongly typed DTOs and validation.

---

# 4. HIGH-LEVEL DATA FLOW

## WhatsApp Ordering

Customer sends WhatsApp message.

Meta WhatsApp Cloud API sends webhook to ASP.NET Core.

ASP.NET Core:

1. Validates webhook authenticity.
2. Parses and normalizes the message.
3. Checks the incoming WhatsApp message ID.
4. Persists the valid event in Neon.
5. Acknowledges receipt.
6. Dispatches processing to n8n when available.

n8n:

1. Loads customer conversation context.
2. Calls the configured AI provider.
3. Determines the customer intent.
4. Calls authorized ASP.NET Core endpoints.
5. Updates conversation state through the API.
6. Sends an appropriate reply through the trusted WhatsApp integration.

ASP.NET Core remains authoritative for:

- Menu prices.
- Availability.
- Cart changes.
- Order totals.
- Inventory.
- Payment statuses.
- Staff authentication.
- Payment approvals.
- Order fulfillment transitions.

The AI model must never directly change verified financial records.

## Payment Processing

Customer chooses payment method.

ASP.NET Core creates a payment attempt.

For Paystack:

- Initialize test transaction.
- Send checkout link.
- Receive secure payment event.
- Verify transaction through Paystack API.
- Atomically update database.
- Trigger order and kitchen notifications.

For manual payment:

- Provide simulated payment details in test mode.
- Receive screenshot.
- Store evidence securely in Cloudinary.
- Extract candidate transaction information when OCR is available.
- Notify authorized restaurant staff.
- Require independent verification or an explicit simulated test approval.
- Update order after authorized approval.

---

# 5. CORE WHATSAPP AI AGENT CAPABILITIES

Develop an assistant capable of understanding:

"Hello"

"Show me the menu"

"What food do you have?"

"I want two jollof rice with chicken"

"Add one Coke"

"Remove the Coke"

"Change the rice to three plates"

"How much is everything?"

"Can you deliver to East Legon?"

"I want to pick up the food"

"I will use Paystack"

"I want to pay through MoMo"

"Here is my payment receipt"

"Have you received my payment?"

"Where is my order?"

"Talk to a person"

The agent must:

- Maintain conversation context.
- Support incomplete conversations.
- Understand updates to a customer's cart.
- Ask for missing details.
- Support human handoff.
- Retrieve live menu data.
- Check item availability.
- Handle multiple food items.
- Avoid fabricated prices.
- Respond concisely and naturally.
- Explain errors understandably.
- Respect restaurant opening hours.
- Handle AI provider timeouts and rate limits.

All AI-generated actions must be validated against typed application contracts.

Use backend-controlled tool allowlists.

Protect against prompt injection and unauthorized tool execution.

---

# 6. RESTAURANT MENU MANAGEMENT

Implement full CRUD functionality for:

- Menu categories.
- Menu items.
- Menu descriptions.
- Prices.
- Food variants.
- Meal sizes.
- Add-ons.
- Availability.
- Food images.
- Allergens.
- Preparation time estimates.
- Promotions.

Store image files in Cloudinary.

Store Cloudinary public IDs, asset IDs, delivery types, resource types, and other necessary metadata in Neon.

For ordinary public menu photos, use public delivery URLs.

Upload and delete menu images through authenticated backend operations.

Staff must be able to upload new menu images from the Angular dashboard.

Show image previews and upload progress.

Do not expose the Cloudinary API secret in Angular environment files.

---

# 7. CART, CHECKOUT AND ORDER MANAGEMENT

Support:

- Add to cart.
- Remove from cart.
- Update quantity.
- Add meal instructions.
- View subtotal.
- Calculate delivery fee.
- Apply permitted discounts.
- Confirm cart.
- Select delivery or pickup.
- Collect delivery details.
- Confirm final amount.
- Generate order number.
- Create pending payment attempt.

Use integer minor currency units for financial calculations.

Create customer-friendly order references such as:

RST-20261026-0001

Use unique internal IDs and enforce database uniqueness.

Order statuses:

- DRAFT.
- AWAITING_CONFIRMATION.
- AWAITING_PAYMENT.
- PAYMENT_UNDER_REVIEW.
- PAID.
- ACCEPTED.
- PREPARING.
- READY.
- OUT_FOR_DELIVERY.
- COMPLETED.
- CANCELLED.
- EXPIRED.
- REFUNDED.

Payment statuses must be modeled separately.

Provide an explicit validated order and payment state machine.

Use atomic database transactions for sensitive changes.

---

# 8. PAYSTACK TEST MODE

Integrate Paystack using the official API.

Initially:

PAYMENT_MODE=TEST

Requirements:

1. Customer confirms an order.
2. Backend verifies the order total.
3. Customer chooses Paystack.
4. Backend creates a unique payment reference.
5. Backend initializes Paystack transaction.
6. n8n sends payment link through WhatsApp.
7. Customer completes simulated checkout.
8. Paystack sends payment notification.
9. Backend validates the signed webhook using the original raw request body.
10. Backend retrieves the transaction from the Paystack verification endpoint.
11. Backend checks transaction success, correct currency, amount, reference, order, and environment.
12. Payment and order status update atomically.
13. Customer receives confirmation.
14. Kitchen receives the verified order.

Support:

- Failed payments.
- Pending payments.
- Duplicate webhooks.
- Incorrect amounts.
- Invalid references.
- Expired orders.
- Delayed successful transactions.
- Safe retry and reconciliation.
- Idempotent payment confirmation.
- Refund handling where applicable.

Never confirm a payment from the checkout redirect alone.

---

# 9. MANUAL MOBILE MONEY AND BANK PAYMENT

Support manual payment channels including:

- MTN Mobile Money.
- Telecel Cash.
- AirtelTigo Money.
- Bank transfer.

Start with simulated payment destinations.

Do not request real money during testing.

## Screenshot Upload and Verification

Customer sends screenshot on WhatsApp.

Backend must:

1. Validate WhatsApp media metadata.
2. Fetch media from the trusted WhatsApp API.
3. Enforce file size and type limits.
4. Validate the actual file format.
5. Calculate a cryptographic hash.
6. Upload the receipt to Cloudinary using authenticated delivery.
7. Save Cloudinary identifiers and metadata in Neon.
8. Extract candidate payment details using a free OCR implementation where practical.
9. Compare details with the pending order.
10. Detect duplicate evidence and transaction references.
11. Create a payment review request.
12. Notify authorized staff in Angular.

OCR results are informational and cannot independently authorize payment.

Payment screenshot review is a two-step process:

A. Read and assess evidence.

B. Independently verify actual receipt of funds using trusted payment records, or explicitly perform simulated approval in development mode.

Only after valid authorization may the payment state become VERIFIED.

## Cloudinary Security

Use `type=authenticated` for sensitive payment receipts when supported by the selected configuration.

Never use ordinary public Cloudinary URLs for payment screenshots.

Generate short-lived authorized access to screenshots only after staff authorization.

Do not store permanent unrestricted receipt URLs in the dashboard.

Implement:

- Secure upload.
- Receipt access control.
- Duplicate detection.
- File retention.
- Deletion.
- Error handling.
- Upload audit trail.

Provide fallback staff review without OCR if image processing exceeds Render Free resources.

---

# 10. ANGULAR ADMIN DASHBOARD

Build a modern, professional restaurant administration dashboard in Angular 22.

Use standalone components, Angular Signals, typed services, Reactive Forms, Angular Router, and Tailwind CSS.

Use feature-oriented architecture.

## Dashboard Pages

### Authentication

- Staff login.
- Logout.
- Session management.
- Role-based navigation.
- Route guards.
- Unauthorized access handling.

### Overview

- Total orders.
- Pending orders.
- Orders awaiting payment.
- Payments under review.
- Verified revenue.
- Active kitchen orders.
- Completed orders.
- Recent activity.

### Menu Management

- Menu categories.
- Food list.
- Create new food.
- Edit food.
- Image upload to Cloudinary through backend.
- Manage availability.
- Update prices.
- Manage meal add-ons.

### Order Management

- View orders.
- Filter and search.
- View ordered items.
- View customer information.
- Update permitted order states.
- Cancel eligible orders.
- Review history.

### Payment Review

- View submitted receipts.
- Request authorized receipt preview.
- Display OCR candidate details.
- Show duplicate evidence warnings.
- Record independent payment verification.
- Approve or reject eligible payments.
- Display audit history.

### Kitchen Dashboard

- New verified orders.
- Accepted orders.
- Preparing orders.
- Ready orders.
- Order details.

### Customer Management

- Customer listing.
- Order history.
- Conversation history subject to permissions.
- Support handoff records.

### Settings

- Restaurant profile.
- Opening hours.
- Payment configuration.
- Delivery fees.
- WhatsApp settings.
- Staff roles.
- General preferences.

### Reports

- Daily orders.
- Verified transactions.
- Payment failures.
- Revenue summaries.
- Pending reconciliation.

## Angular Development Standards

Use:

- Lazy-loaded feature routes.
- Standalone components.
- Angular Signals where appropriate.
- RxJS for asynchronous API operations.
- Typed Reactive Forms.
- HTTP interceptors.
- Route guards.
- Reusable UI components.
- Loading skeletons.
- Empty states.
- Error states.
- Form validation.
- Responsive layouts.
- Accessible controls.

Never place database credentials, Paystack secret keys, or Cloudinary API secrets in Angular source code.

The Angular application communicates only with authorized ASP.NET Core APIs.

For order updates, support efficient polling during free-tier testing, with an option to use SignalR after production hosting becomes reliable.

---

# 11. ASP.NET CORE REST API DESIGN

Create versioned API endpoints under:

/api/v1/

Required endpoint groups:

## Authentication

POST /auth/login

POST /auth/logout

GET /auth/me

Use secure authentication, appropriate cookie/token handling, and role-based authorization.

## Restaurants

GET /restaurant

PUT /restaurant

## Menu

GET /menu/categories

POST /menu/categories

GET /menu/items

GET /menu/items/{id}

POST /menu/items

PUT /menu/items/{id}

DELETE /menu/items/{id}

PATCH /menu/items/{id}/availability

## Cart

GET /carts/current

POST /carts/items

PATCH /carts/items/{id}

DELETE /carts/items/{id}

## Orders

POST /orders/checkout

GET /orders

GET /orders/{id}

PATCH /orders/{id}/status

POST /orders/{id}/cancel

## Payments

POST /payments/paystack/initialize

GET /payments/{id}/status

GET /payments/reviews

POST /payments/reviews/{id}/approve

POST /payments/reviews/{id}/reject

## Media

POST /media/menu-images

POST /media/payment-evidence

GET /media/payment-evidence/{id}/access

DELETE /media/{id}

## Customers

GET /customers

GET /customers/{id}

GET /customers/{id}/orders

## Webhooks

GET /webhooks/whatsapp

POST /webhooks/whatsapp

POST /webhooks/paystack

## Events

GET /internal/events/pending

POST /internal/events/{id}/retry

Provide secure internal endpoints for n8n to request permitted business operations.

Do not expose internal workflow credentials to the Angular application.

The endpoint paths above are a starting contract; refine them for consistency and security before implementation.

Implement request and response DTOs, validators, documented error responses, pagination, and appropriate HTTP status codes.

---

# 12. ENTITY FRAMEWORK CORE AND NEON

Use EF Core 10 with PostgreSQL.

Create two Neon database projects where free account limits permit:

A. restaurant-automation-dev

- n8n database.
- n8n workflow metadata.
- Credentials metadata.
- Execution history.

B. restaurant-business-dev

- Business entities.
- Orders.
- Payments.
- Customers.
- Conversations.
- Receipt metadata.
- Durable events.
- Audit logs.

Business data must use EF Core migrations.

n8n internal tables must be managed by n8n, not application EF Core migrations.

Use Neon-approved SSL/TLS connection parameters.

Use Npgsql connection pooling responsibly.

Implement separate credentials and privileges.

Use correct direct connections for migrations and supported pooled connections for application queries.

Do not disable TLS certificate validation.

## Required Tables

- Restaurants.
- RestaurantSettings.
- StaffUsers.
- Roles.
- Customers.
- CustomerAddresses.
- MenuCategories.
- MenuItems.
- MenuVariants.
- MenuAddons.
- Carts.
- CartItems.
- Orders.
- OrderItems.
- OrderStatusHistories.
- PaymentAttempts.
- PaymentTransactions.
- PaymentEvidence.
- PaymentReviews.
- ConversationSessions.
- ConversationMessages.
- WebhookEvents.
- OutboxEvents.
- NotificationDeliveries.
- InventoryReservations.
- MediaAssets.
- AuditLogs.

Provide relationships, unique constraints, indexes, audit timestamps, concurrency controls, and deletion policies.

Use C# domain models, EF Core configurations, DbContext, and executable migrations.

---

# 13. REQUIRED n8n WORKFLOWS

Generate actual importable n8n JSON workflows.

Workflow 01 — WhatsApp Incoming Message Processing.

Workflow 02 — AI Conversation and Intent Recognition.

Workflow 03 — Menu Search and Availability.

Workflow 04 — Cart Operations.

Workflow 05 — Checkout and Order Creation.

Workflow 06 — Paystack Initialization.

Workflow 07 — Verified Payment Notifications.

Workflow 08 — Manual Receipt Processing.

Workflow 09 — Manual Payment Review Notifications.

Workflow 10 — Kitchen Notifications.

Workflow 11 — Order Status Tracking.

Workflow 12 — Human Support Handoff.

Workflow 13 — Failed Event Recovery.

Workflow 14 — Daily Restaurant Reporting.

Every workflow must:

- Use real supported n8n node types.
- Contain valid importable JSON.
- Be compatible with a pinned n8n version.
- Use protected credentials.
- Call ASP.NET Core API endpoints for business operations.
- Support idempotency.
- Define failure handling.
- Document inputs and outputs.
- Include test fixtures.
- Avoid directly authorizing financial state changes through AI or n8n logic.

ASP.NET Core is responsible for trusted payment processing.

n8n is responsible for conversation orchestration, routing, and notifications.

---

# 14. RENDER FREE DEPLOYMENT

Deploy three applications from GitHub.

## restaurant-api

Type: Render Web Service.

Runtime: Docker.

Framework: ASP.NET Core 10.

Dockerfile: Multi-stage build using official Microsoft SDK and ASP.NET runtime images.

Requirements:

- Listen on 0.0.0.0.
- Respect Render's PORT variable.
- Use an appropriate health endpoint.
- Connect to Neon using TLS.
- Read secrets from Render environment variables.
- Avoid storing durable files locally.
- Log startup and database connectivity failures.

## restaurant-n8n

Type: Render Web Service.

Runtime: Docker.

Use an official pinned n8n image.

Store n8n persistent data in Neon PostgreSQL.

Use supported configuration variables including:

DB_TYPE
DB_POSTGRESDB_HOST
DB_POSTGRESDB_PORT
DB_POSTGRESDB_DATABASE
DB_POSTGRESDB_USER
DB_POSTGRESDB_PASSWORD
DB_POSTGRESDB_SCHEMA
N8N_ENCRYPTION_KEY
WEBHOOK_URL
N8N_EDITOR_BASE_URL
N8N_HOST
N8N_PROTOCOL
N8N_PROXY_HOPS

Add verified SSL and execution-retention settings based on the pinned n8n version.

Never allow the encryption key to change unexpectedly.

## restaurant-admin

Type: Render Static Site.

Build the Angular application using Angular CLI.

Configure the correct Angular build output directory from angular.json.

Provide routing fallback configuration so deep links such as `/orders/123` load correctly.

Never use a Node.js server merely to host the static Angular dashboard.

## Free Tier Limitations

Render Free services may spin down, producing webhook timeouts and delayed responses.

Free services also have usage limits and ephemeral storage.

Implement durable webhook ingestion and replay.

Do not rely solely on background services running continuously on sleeping Render instances.

Explain how to test event recovery after cold starts.

Require always-on infrastructure and an appropriate persistent processing mechanism for production.

---

# 15. ANGULAR UNIT AND COMPONENT TESTING

Use the testing tools supported by the selected Angular CLI version.

Default:

- Vitest.
- Angular TestBed.
- Angular testing utilities.
- HttpTestingController or supported HTTP testing utilities.
- Mocked services and controlled test fixtures.

Provide tests for:

### Authentication

- Login form validation.
- Login success and failure.
- Route guards.
- Role-based navigation.
- Logout.

### Menu

- Menu rendering.
- Add and edit forms.
- Price validation.
- Food availability toggles.
- Cloudinary upload UI.
- Upload errors.

### Orders

- Order list rendering.
- Filters.
- Detail views.
- Status changes.
- Invalid transitions.
- Loading and empty states.

### Payments

- Payment status labels.
- Receipt review presentation.
- Approval confirmation.
- Rejection form validation.
- Unauthorized actions hidden or blocked.

### Dashboard

- Summary cards.
- Error states.
- API loading.
- Responsive layouts.
- Navigation.

### Angular Services

- Correct API requests.
- Typed response handling.
- Interceptors.
- Authentication errors.
- Network failures.

Use repeatable tests without depending on actual Paystack or Cloudinary credentials.

Provide coverage reporting and CI execution commands.

---

# 16. C# UNIT AND API INTEGRATION TESTING

Use:

- xUnit.
- Microsoft.AspNetCore.Mvc.Testing.
- WebApplicationFactory.
- HttpClient.
- Mocked external service adapters.
- PostgreSQL Testcontainers where feasible.
- Dedicated isolated test databases.

Do not run destructive tests against staging or production Neon databases.

## Unit Tests

Test:

- Cart calculations.
- Delivery fee calculations.
- Order status transitions.
- Payment state transitions.
- Price snapshots.
- Discount rules.
- Input validation.
- Duplicate detection.
- Payment amount conversions.

## API Integration Tests

Test the real ASP.NET Core request pipeline.

Include:

1. Authentication and authorization.
2. Menu CRUD.
3. Customer management.
4. Cart creation and update.
5. Checkout.
6. Database persistence.
7. EF Core migration correctness.
8. Paystack initialization.
9. Signed payment webhook acceptance.
10. Invalid webhook signature rejection.
11. Transaction verification.
12. Incorrect amount rejection.
13. Duplicate transaction prevention.
14. Manual receipt submission.
15. Cloudinary upload service using controlled test doubles.
16. Protected receipt access.
17. Authorized payment approval.
18. Unauthorized approval rejection.
19. Kitchen status transitions.
20. Order tracking.
21. Duplicate WhatsApp webhook processing.
22. Durable event recovery.
23. Validation errors and exception handling.

Use realistic PostgreSQL behavior for relational and transactional tests.

Do not rely on EF Core InMemory provider to validate PostgreSQL constraints or transaction behavior.

Tests must use deterministic fixtures and clean up their data.

Provide test commands, expected results, and CI configuration.

---

# 17. END-TO-END TESTING

Use Playwright to test the Angular dashboard against a controlled test backend.

Required scenarios:

- Staff login.
- Create a menu category.
- Add food and image.
- Update availability.
- View a simulated incoming order.
- Review order details.
- View manual payment evidence.
- Approve a simulated payment.
- Reject a simulated payment.
- Send order to kitchen.
- Change food preparation status.
- Confirm customer order tracking.
- Log out.

Test desktop and mobile viewport behavior.

Mock or simulate external services as necessary.

Provide test data reset commands.

---

# 18. CI/CD WITH GITHUB ACTIONS

Configure GitHub Actions to run tests when code is pushed or pull requests are opened.

Suggested workflow:

1. Restore .NET dependencies.
2. Build the backend.
3. Run .NET unit tests.
4. Run ASP.NET Core API integration tests.
5. Install Angular dependencies using the lockfile.
6. Run Angular unit/component tests.
7. Build Angular for production.
8. Run Playwright tests in an appropriate test environment.
9. Perform static analysis and dependency checks.
10. Prevent deployment on failing required checks.

Only approved branches should trigger deployment.

Never expose production secrets to untrusted pull-request workflows.

Do not require Paystack live credentials in CI.

Use appropriate test credentials, fakes, and isolated database environments.

---

# 19. COMPLETE PROJECT STRUCTURE

Create a monorepo with the following architecture:

restaurant-whatsapp-ai/

### backend/

Restaurant.slnx

src/
- Restaurant.API/
- Restaurant.Application/
- Restaurant.Domain/
- Restaurant.Infrastructure/

tests/
- Restaurant.UnitTests/
- Restaurant.IntegrationTests/

Dockerfile

### frontend/

restaurant-admin/

src/app/
- core/
  - auth/
  - guards/
  - interceptors/
  - services/
- shared/
  - components/
  - pipes/
  - directives/
  - models/
- features/
  - dashboard/
  - menu/
  - orders/
  - payments/
  - customers/
  - kitchen/
  - reports/
  - settings/
- layouts/
- app.routes.ts
- app.config.ts

src/environments/

e2e/

angular.json
package.json
tailwind configuration

### automation/

workflows/
- 01-whatsapp-incoming.json
- 02-ai-conversation.json
- 03-menu.json
- 04-cart.json
- 05-checkout.json
- 06-paystack-initialize.json
- 07-payment-notification.json
- 08-manual-evidence.json
- 09-payment-review.json
- 10-kitchen.json
- 11-tracking.json
- 12-handoff.json
- 13-recovery.json
- 14-reporting.json

Dockerfile

### infrastructure/

render/
- render.yaml

docker/
- docker-compose.yml

### database/

- migrations/
- seeds/
- scripts/

### tests/

- fixtures/
- webhook-payloads/
- payment-scenarios/

### docs/

- architecture.md
- installation.md
- database.md
- render-deployment.md
- angular-dashboard.md
- backend-api.md
- cloudinary.md
- n8n-workflows.md
- paystack.md
- security.md
- testing.md
- production-checklist.md

### Root

- .gitignore
- .env.example
- README.md
- GitHub Actions workflows

The backend's EF Core migrations may live inside Restaurant.Infrastructure rather than a duplicated root migrations folder. Choose one canonical migration location.

---

# 20. ENVIRONMENT VARIABLES

Provide actual configuration files and secure placeholders.

## ASP.NET Core

ASPNETCORE_ENVIRONMENT=Development

ConnectionStrings__DefaultConnection=...

Paystack__Mode=Test
Paystack__SecretKey=...
Paystack__PublicKey=...

Cloudinary__CloudName=...
Cloudinary__ApiKey=...
Cloudinary__ApiSecret=...

Meta__AccessToken=...
Meta__PhoneNumberId=...
Meta__VerifyToken=...
Meta__AppSecret=...

AI__Provider=Groq
AI__ApiKey=...
AI__Model=...

Automation__BaseUrl=...
Automation__SharedSecret=...

Payments__AllowLivePayments=false

## Angular

Configure public API base URL through a deliberate environment or runtime configuration mechanism.

Do not place server secrets in Angular environment files.

## n8n

DB_TYPE=postgresdb
DB_POSTGRESDB_HOST=...
DB_POSTGRESDB_PORT=5432
DB_POSTGRESDB_DATABASE=...
DB_POSTGRESDB_USER=...
DB_POSTGRESDB_PASSWORD=...
DB_POSTGRESDB_SCHEMA=public

N8N_ENCRYPTION_KEY=...
WEBHOOK_URL=...
N8N_EDITOR_BASE_URL=...

Add appropriate SSL settings, proxy settings, and execution-data retention settings.

Verify the exact environment configuration against current official documentation.

---

# 21. SECURITY REQUIREMENTS

Implement:

- HTTPS.
- Secure JWT or cookie-based authentication.
- Role-based authorization.
- Policy-based permissions for financial operations.
- Secure secrets management.
- API rate limiting.
- Request validation.
- Webhook signature verification.
- SQL parameterization.
- Cross-origin access restrictions.
- Appropriate CSRF protection when cookie authentication is used.
- Safe file upload handling.
- Authenticated receipt access.
- Payment idempotency.
- Database transaction safety.
- Structured audit logging.
- Prompt injection protection.
- Sensitive-data minimization.
- Backup and recovery procedures.
- Data retention and deletion controls.

A customer payment screenshot is not proof of payment.

A Groq response is not an authorized financial decision.

An n8n workflow result is not proof of successful payment.

Only trusted verification and authorized application rules can change a payment to VERIFIED.

---

# 22. DEVELOPMENT PHASES

Implement the system in the following order.

### Phase 1 — Architecture and Setup

- Create GitHub monorepo.
- Set up .NET 10 solution.
- Set up Angular 22 application.
- Create Neon databases.
- Configure Docker.
- Configure environment files.
- Set up GitHub Actions.

### Phase 2 — Backend Foundation

- Implement Clean Architecture.
- Configure EF Core.
- Implement migrations.
- Create base entities.
- Configure Neon.
- Implement authentication.
- Add health endpoints.
- Add OpenAPI.

### Phase 3 — n8n and Webhooks

- Deploy n8n on Render.
- Connect n8n to Neon.
- Create ASP.NET Core webhook endpoints.
- Implement durable event recording.
- Test event dispatch.
- Test redeployment and cold starts.

### Phase 4 — WhatsApp and AI

- Configure Meta WhatsApp test environment.
- Integrate Groq.
- Manage customer conversations.
- Implement menu enquiries.
- Implement customer intent recognition.

### Phase 5 — Menu and Cart

- Menu CRUD.
- Cloudinary menu images.
- Shopping cart.
- Checkout.
- Delivery charges.
- Customer details.

### Phase 6 — Payments

- Paystack Test Mode.
- Webhook verification.
- Transaction reconciliation.
- Simulated Mobile Money.
- Cloudinary receipt uploads.
- Staff payment approval.

### Phase 7 — Angular Dashboard

- Authentication.
- Dashboard.
- Menu management.
- Order management.
- Payment verification.
- Kitchen dashboard.
- Customer management.
- Reports.

### Phase 8 — Automation and Tracking

- Order notifications.
- Kitchen alerts.
- Order tracking.
- Human handoff.
- Event recovery.
- Sales reporting.

### Phase 9 — Testing

- Angular unit/component tests.
- C# unit tests.
- API integration tests.
- Database integration tests.
- Playwright E2E tests.
- n8n workflow tests.
- Payment security tests.
- Render cold-start recovery tests.

### Phase 10 — Production Preparation

- Security review.
- Database backup and recovery.
- Cloudinary storage review.
- Reliable production hosting plan.
- Payment reconciliation testing.
- Webhook resilience.
- Load testing.
- Incident response documentation.
- Production launch checklist.

For every phase, provide:

1. Objective.
2. Architecture explanation.
3. Complete source files.
4. Commands.
5. Database changes.
6. Configuration.
7. Deployment steps.
8. Tests.
9. Expected results.
10. Common failure solutions.
11. Acceptance criteria.

Do not use pseudocode when executable code has been requested.

Do not claim tests passed unless they were actually executed.

Do not silently skip incomplete features.

---

# 23. PRODUCTION READINESS

The application must remain in development or test mode until explicitly authorized for production.

Before launch require:

- Reliable always-on web hosting.
- Stable automation execution.
- Verified WhatsApp production account.
- Paystack live activation.
- Appropriate manual payment reconciliation.
- Protected Cloudinary receipt storage.
- Secured production Neon environment.
- Backup verification.
- Production monitoring.
- Security testing.
- Staff permissions review.
- Automated test execution.
- Live webhook smoke testing.
- Refund and cancellation procedures.
- Documented privacy and retention policies.

Provide separate development, staging, and production settings.

Never automatically switch to live Paystack payments.

---

# 24. START IMPLEMENTATION

Begin with Phase 1.

Produce the following:

1. Complete architecture diagram.
2. ASP.NET Core Clean Architecture solution structure.
3. Angular feature architecture.
4. Neon database design.
5. Cloudinary storage architecture.
6. Render hosting architecture.
7. Complete monorepo layout.
8. Verified compatible dependencies.
9. Initial C# ASP.NET Core Web API implementation.
10. Initial Angular admin dashboard implementation.
11. EF Core Neon connection setup.
12. Dockerfiles and Docker Compose.
13. Render deployment configuration.
14. C# unit test project.
15. C# API integration test project.
16. Angular Vitest test configuration.
17. GitHub Actions CI workflow.
18. Commands to build and run locally.
19. Commands to test each service.
20. Phase 1 acceptance checklist.

The FIRST working milestone must be:

- An ASP.NET Core backend running locally.
- A Neon PostgreSQL connection that works.
- An Angular dashboard communicating with the backend.
- A functional health-check endpoint.
- A passing Angular component test.
- A passing ASP.NET Core API integration test.
- A tested deployment configuration for Render.

After that, integrate n8n, WhatsApp, AI conversations, orders, and payments.

Build the application step by step using complete, maintainable, real code.






//// NEW LINE OF COOMAD TO CONTINUE WITH 

I want to adjust the development order without losing or overwriting any work you've already completed.

First, inspect the existing project and summarize:
- What has been implemented.
- What is working.
- What remains incomplete.
- Which tests have actually passed.

Keep all existing Angular, ASP.NET Core, Neon, and other project files.

**New priority: Get n8n working before continuing with the full backend and Angular dashboard.**

The next milestone should be:

1. Configure Neon PostgreSQL for n8n persistence.
2. Prepare n8n for Render Free deployment.
3. Create and test the first n8n webhook workflow.
4. Integrate Groq AI.
5. Connect the Meta WhatsApp test environment.
6. Create a basic restaurant AI agent that responds to greetings, menu requests, and simple orders using mock menu data.

Once that is working, continue integrating the ASP.NET Core backend, real database-driven menus, Paystack payments, Cloudinary, and the Angular dashboard.

Do not rebuild completed components unnecessarily.

Do not generate all workflows at once. Implement and test each milestone before moving forward.

For now, focus on getting the n8n automation running successfully.