-- Run once in the SQL Editor of the separate restaurant-business-dev project.
-- Select database restaurant_business_dev. Never run this in the n8n project.
-- The password is generated inside PostgreSQL and appears only in this query's
-- private result tab. Copy it to a password manager; never share a screenshot.
BEGIN;

DO $business_guard$
BEGIN
    IF current_database() <> 'restaurant_business_dev' THEN
        RAISE EXCEPTION 'Wrong database: select restaurant_business_dev';
    END IF;
    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'restaurant_api_test') THEN
        RAISE EXCEPTION 'Runtime role already exists; do not create it twice';
    END IF;
END $business_guard$;

CREATE TEMP TABLE _restaurant_api_generated_password (
    password text NOT NULL
) ON COMMIT DROP;

-- Two independent version-4 UUIDs provide more than 240 bits of randomness.
INSERT INTO _restaurant_api_generated_password (password)
SELECT replace(gen_random_uuid()::text, '-', '') ||
       replace(gen_random_uuid()::text, '-', '');

DO $create_runtime_role$
DECLARE
    generated_password text;
BEGIN
    SELECT password INTO STRICT generated_password
    FROM _restaurant_api_generated_password;
    EXECUTE format('CREATE ROLE restaurant_api_test WITH LOGIN PASSWORD %L', generated_password);
END $create_runtime_role$;

GRANT CONNECT ON DATABASE restaurant_business_dev TO restaurant_api_test;
GRANT USAGE ON SCHEMA public TO restaurant_api_test;
GRANT SELECT ON TABLE "Restaurants", "MenuCategories", "MenuItems" TO restaurant_api_test;
GRANT SELECT, INSERT, UPDATE ON TABLE "MetaWebhookEvents" TO restaurant_api_test;

-- Abort if the role inherited broader privileges than this test API requires.
DO $rights_guard$
BEGIN
    IF EXISTS (
        SELECT 1 FROM pg_auth_members m
        JOIN pg_roles child ON child.oid = m.member
        JOIN pg_roles parent ON parent.oid = m.roleid
        WHERE child.rolname = 'restaurant_api_test' AND parent.rolname = 'neon_superuser'
    ) OR has_table_privilege('restaurant_api_test', '"MetaWebhookEvents"', 'DELETE')
      OR has_table_privilege('restaurant_api_test', '"MenuItems"', 'INSERT')
      OR has_schema_privilege('restaurant_api_test', 'public', 'CREATE') THEN
        RAISE EXCEPTION 'Runtime role has unexpected write or schema privileges';
    END IF;
END $rights_guard$;

-- Copy this one result privately into a password manager. Do not screenshot it.
SELECT password AS copy_to_password_manager_do_not_share
FROM _restaurant_api_generated_password;

-- Safe verification result; this contains no password.
SELECT current_database() AS database_name,
       'restaurant_api_test' AS runtime_role,
       has_table_privilege('restaurant_api_test', '"Restaurants"', 'SELECT') AS can_read_catalog,
       has_table_privilege('restaurant_api_test', '"MetaWebhookEvents"', 'INSERT') AS can_store_meta_event,
       has_table_privilege('restaurant_api_test', '"MetaWebhookEvents"', 'UPDATE') AS can_update_meta_event,
       NOT has_table_privilege('restaurant_api_test', '"MetaWebhookEvents"', 'DELETE') AS cannot_delete_meta_event;

COMMIT;
