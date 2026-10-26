import assert from 'node:assert/strict';
import { docker, loadPrivateEnv, ready } from './lib.mjs';
const env=loadPrivateEnv(new URL('../.env.local',import.meta.url));
assert.equal(env['DB_POSTGRESDB_HOST'],'automation-db','This helper is for the separate local database. Use the documented Neon setup for remote persistence.');
assert.equal(env['DB_POSTGRESDB_DATABASE'],'restaurant_automation_dev');
assert.ok(env['N8N_ENCRYPTION_KEY']?.length>=32,'Run setup-local.mjs first.');
const args=['compose','--env-file','automation/.env.local','-f','automation/compose.yml'];
// Explicit private settings take priority over ambient shell values from other projects.
docker([...args,'--profile','local-db','up','--detach','--wait','automation-db'],{env,quiet:false});
docker([...args,'up','--detach','--build','n8n'],{env,quiet:false});
await ready(`http://127.0.0.1:${env['AUTOMATION_PORT']??'5678'}`);
console.log(`n8n is ready at http://localhost:${env['AUTOMATION_PORT']??'5678'}. Existing data and encryption key retained.`);
