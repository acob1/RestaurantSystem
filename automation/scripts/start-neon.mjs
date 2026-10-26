// Starts only the dedicated development Neon-backed workspace, after a verified read-only check.
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { docker, loadPrivateEnv, ready } from './lib.mjs';
const env=loadPrivateEnv(new URL('../.env.neon',import.meta.url));
assert.equal(env['COMPOSE_PROJECT_NAME'],'restaurant-automation-neon','Use the separate Neon Compose project.');
assert.notEqual(env['AUTOMATION_PORT'],'5678','Keep port 5678 for the existing local instance.');
assert.equal(env['DB_POSTGRESDB_SSL_ENABLED'],'true');
assert.ok(env['N8N_ENCRYPTION_KEY']?.length>=32&&!env['N8N_ENCRYPTION_KEY'].includes('REPLACE'),'Set a stable private encryption key.');
const check=spawnSync(process.execPath,[fileURLToPath(new URL('./check-neon.mjs',import.meta.url))],{stdio:'inherit'});
if(check.error) throw check.error;
if(check.status!==0) process.exit(check.status??1);
const args=['compose','--env-file','automation/.env.neon','-f','automation/compose.yml'];
docker([...args,'up','--detach','--build','n8n'],{env,quiet:false});
await ready(`http://127.0.0.1:${env['AUTOMATION_PORT']}`,600000);
console.log(`Neon-backed n8n is ready at http://localhost:${env['AUTOMATION_PORT']}. Original local workspace retained.`);
