-- Generated from the two EF Core migrations for the separate business database.
-- Run only in Neon SQL Editor with database restaurant_business_dev selected.
-- This guard must remain before all schema changes.
DO $business_guard$
BEGIN
    IF current_database() <> 'restaurant_business_dev' THEN
        RAISE EXCEPTION 'Wrong database: select restaurant_business_dev before running this script';
    END IF;
END $business_guard$;

CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026115543_InitialCatalog') THEN
    CREATE TABLE "MenuCategories" (
        "Id" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        CONSTRAINT "PK_MenuCategories" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026115543_InitialCatalog') THEN
    CREATE TABLE "Restaurants" (
        "Id" uuid NOT NULL,
        "Name" character varying(160) NOT NULL,
        "Currency" character varying(3) NOT NULL,
        "TimeZone" character varying(80) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Restaurants" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026115543_InitialCatalog') THEN
    CREATE TABLE "MenuItems" (
        "Id" uuid NOT NULL,
        "CategoryId" uuid NOT NULL,
        "Name" character varying(160) NOT NULL,
        "Description" character varying(2000) NOT NULL,
        "PriceMinor" bigint NOT NULL,
        "Available" boolean NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_MenuItems" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_MenuItems_PriceMinor" CHECK ("PriceMinor" > 0 AND "PriceMinor" <= 100000000),
        CONSTRAINT "FK_MenuItems_MenuCategories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "MenuCategories" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026115543_InitialCatalog') THEN
    CREATE UNIQUE INDEX "IX_MenuCategories_Name" ON "MenuCategories" ("Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026115543_InitialCatalog') THEN
    CREATE UNIQUE INDEX "IX_MenuItems_CategoryId_Name" ON "MenuItems" ("CategoryId", "Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026115543_InitialCatalog') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261026115543_InitialCatalog', '10.0.13');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261027023343_MetaTestInbox') THEN
    CREATE TABLE "MetaWebhookEvents" (
        "Id" uuid NOT NULL,
        "MessageId" character varying(200) NOT NULL,
        "PhoneNumberId" character varying(80) NOT NULL,
        "SenderWaId" character varying(32) NOT NULL,
        "Text" character varying(1000) NOT NULL,
        "ReceivedAt" timestamp with time zone NOT NULL,
        "State" character varying(24) NOT NULL,
        CONSTRAINT "PK_MetaWebhookEvents" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261027023343_MetaTestInbox') THEN
    CREATE UNIQUE INDEX "IX_MetaWebhookEvents_MessageId" ON "MetaWebhookEvents" ("MessageId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261027023343_MetaTestInbox') THEN
    CREATE INDEX "IX_MetaWebhookEvents_State_ReceivedAt" ON "MetaWebhookEvents" ("State", "ReceivedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261027023343_MetaTestInbox') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261027023343_MetaTestInbox', '10.0.13');
    END IF;
END $EF$;
COMMIT;

-- Expected result after a successful first or repeated run: 2 migrations, 4 tables.
SELECT current_database() AS database_name,
       (SELECT count(*) FROM "__EFMigrationsHistory") AS applied_migrations,
       (SELECT count(*) FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name IN ('Restaurants', 'MenuCategories', 'MenuItems', 'MetaWebhookEvents')) AS expected_tables;
