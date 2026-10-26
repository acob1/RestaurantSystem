# PostgreSQL and Neon

The canonical EF migration location is `backend/src/Restaurant.Infrastructure/Migrations`. Do not duplicate migrations under root `database/`. n8n manages its own internal schema. Do not run business migrations against the automation database.

## Provision development databases

In your Neon account, create `restaurant-business-dev` and `restaurant-automation-dev` projects if free limits permit, or separate databases/roles in one project. Separate roles must not be able to access the other application's database. Keep staging and production in separate projects/branches and credentials. Create database-owner/migrator credentials separate from least-privilege runtime credentials.

For application queries, use the supplied pooled hostname; for EF migrations use the direct hostname. Npgsql requires a semicolon connection string, not a `postgres://` URI:

```sh
export ConnectionStrings__DefaultConnection='Host=ep-REPLACE.REGION.aws.neon.tech;Database=restaurant_business_dev;Username=REPLACE;Password=REPLACE;SSL Mode=VerifyFull;Maximum Pool Size=10'
ASPNETCORE_ENVIRONMENT=Staging dotnet run --project backend/src/Restaurant.API --no-launch-profile -- --migrate
# Switch to the pooled hostname and least-privilege runtime role before serving requests.
ASPNETCORE_ENVIRONMENT=Staging dotnet run --project backend/src/Restaurant.API --no-launch-profile --urls http://localhost:5080
curl --fail http://localhost:5080/health/ready
```

Use a secret manager or environment injection instead of shell history for real passwords. Hosted configuration refuses SSL modes other than VerifyFull. No certificate-validation bypass is used. The local-only default uses SSL Disable for Docker on loopback. Pool maximum is clamped to 10 and commands/timeouts are bounded.

Phase 1 migrations create Restaurants, MenuCategories, and MenuItems. Category names are unique; menu item category/name pairs are unique; positive bounded prices are enforced in PostgreSQL; category deletion is restricted; prices are bigint pesewas; menu concurrency uses PostgreSQL xmin. Restaurant reads deliberately use SingleOrDefault to enforce the current single-restaurant deployment assumption. Multi-restaurant tenancy requires an explicit tenant key/filter and membership authorization in a later design change.

## Planned relational model

The following is the target design, **not a claim that these tables already exist**. Each later phase must add executable EF migrations and relational tests.

| Aggregate | Tables and relationships | Constraints and indexes |
| --- | --- | --- |
| Restaurant | Restaurants 1:1 RestaurantSettings; restaurant 1:N StaffUsers | unique staff normalized email; audit UTC timestamps; optimistic concurrency |
| Staff access | Roles N:M StaffUsers through StaffUserRoles | unique role name, unique membership pair; finance permission policy |
| Customers | Customers 1:N CustomerAddresses, ConversationSessions, Carts, Orders | unique normalized WhatsApp phone; scoped conversation access |
| Catalog | MenuCategories 1:N MenuItems; items 1:N MenuVariants and MenuAddons | nonnegative bounded minor prices, unique category/name, no destructive order cascade |
| Carts | Carts 1:N CartItems; items reference menu item/variant | quantity bounds, cart version, unique permitted line identity; expiry index |
| Orders | Orders 1:N OrderItems and OrderStatusHistories | unique public reference; immutable price/name/add-on snapshots; customer/time and status/time indexes |
| Payments | Orders 1:N PaymentAttempts; attempts 1:N PaymentTransactions, PaymentEvidence, PaymentReviews | unique provider reference and provider transaction ID; currency/amount/environment checks; separate state machine and concurrency |
| Media | MediaAssets referenced by MenuItems and PaymentEvidence | provider asset ID uniqueness, receipt SHA-256 index, no public receipt URL |
| Reviews | PaymentReviews reference attempt, evidence, reviewer staff | evidence assessment and independent verification recorded separately; transaction-reference duplicate detection |
| Conversations | ConversationSessions 1:N ConversationMessages | unique provider message ID, session/version, support handoff state; retention index |
| Events | WebhookEvents, OutboxEvents, NotificationDeliveries | unique source/event ID, unique delivery/event/channel key, lease/retry/next-attempt index, dead-letter metadata |
| Inventory | InventoryReservations reference order and menu item/variant | positive reserved quantities, unique reservation key, expiry index, locked checkout transaction |
| Audit | AuditLogs reference actor/resource IDs | append-only events, timestamp/actor/resource indexes; redact PII and secrets |

Order and financial tables will use restrict/no-action deletion, immutable financial snapshots, explicit retention and anonymization. A checkout transaction will reserve inventory, persist an order and attempts, and write outbox events atomically. Confirmation will lock/check attempt and order, enforce uniqueness, compare amount/currency/reference/environment, transition states, and write history/outbox atomically. Public references will use a database-backed daily sequence with uniqueness; never `COUNT + 1`.

Backups: use isolated `pg_dump` with a direct verified TLS connection, store encrypted off-platform, and restore into an isolated database. Verify row counts, migration history, and reconciliation invariants before accepting a backup. Neon free retention is not a substitute for a tested recovery procedure.

References: [Neon connections](https://neon.com/docs/connect/connect-from-any-app), [Npgsql EF provider](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/10.0.3).
