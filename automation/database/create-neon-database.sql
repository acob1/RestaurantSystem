-- Run once with psql as an authorized Neon project owner on a development project.
-- This creates only the dedicated automation role/database. It never touches business tables.
-- \password prompts securely; no password appears in this file or shell arguments.
\set ON_ERROR_STOP on
CREATE ROLE restaurant_automation LOGIN;
\password restaurant_automation
CREATE DATABASE restaurant_automation_dev OWNER restaurant_automation;
REVOKE ALL ON DATABASE restaurant_automation_dev FROM PUBLIC;
GRANT CONNECT ON DATABASE restaurant_automation_dev TO restaurant_automation;
\connect restaurant_automation_dev
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
GRANT USAGE, CREATE ON SCHEMA public TO restaurant_automation;
