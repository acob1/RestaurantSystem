// Installs only the first development workflow into the dedicated local automation stack.
// Refuses to replace a pre-existing workflow or credential. Never connects to Neon.
import assert from 'node:assert/strict';
import { credential, docker, loadPrivateEnv, ready, workflow } from './lib.mjs';
const file=new URL('../.env.local',import.meta.url);
const env=loadPrivateEnv(file);
assert.equal(env['DB_POSTGRESDB_HOST'],'automation-db','This bootstrap is local-only. Import through the editor for Neon/Render.');
assert.equal(env['DB_POSTGRESDB_DATABASE'],'restaurant_automation_dev');
assert.equal(env['DB_POSTGRESDB_USER'],'automation');
assert.ok(env['RESTAURANT_WEBHOOK_TOKEN']?.length>=32,'Run setup-local.mjs first.');
const compose=['compose','--env-file','automation/.env.local','-f','automation/compose.yml'];
const localCompose=(args,options={})=>docker([...compose,...args],{...options,env});
const server=localCompose(['ps','--quiet','n8n']);
const database=localCompose(['--profile','local-db','ps','--quiet','automation-db']);
assert.ok(server&&database,'Start the standalone local stack first.');
const base=`http://127.0.0.1:${env['AUTOMATION_PORT']??'5678'}`;
await ready(base);
const existing=docker(['exec',database,'psql','-U','automation','-d','restaurant_automation_dev','-Atc',
  "SELECT (SELECT count(*) FROM workflow_entity WHERE id='restaurantWebhook01') + (SELECT count(*) FROM credentials_entity WHERE id='restaurantHeaderAuth01')"]);
if(Number(existing)!==0) {
  console.log('Existing restaurant workflow/credential retained. Use the editor to make deliberate updates.');
  process.exit(0);
}
localCompose(['stop','n8n']);
try {
  localCompose(['run','--rm','--no-deps','-T','--entrypoint','/bin/sh','n8n','-c',
    'umask 077; cat > /tmp/restaurant-credentials.json; n8n import:credentials --input=/tmp/restaurant-credentials.json; status=$?; rm -f /tmp/restaurant-credentials.json; exit "$status"'],
    {input:JSON.stringify(credential(env['RESTAURANT_WEBHOOK_TOKEN']))});
  localCompose(['run','--rm','--no-deps','-T','--entrypoint','n8n','n8n','import:workflow','--input=/opt/restaurant/workflows/01-development-inbox.json']);
  localCompose(['run','--rm','--no-deps','-T','--entrypoint','n8n','n8n','publish:workflow',`--id=${workflow.id}`]);
} finally {
  localCompose(['start','n8n']);
}
await ready(base);
console.log('First development workflow imported and published. Secrets were not printed.');
console.log(`Open ${base} to create your own owner account if setup is still pending.`);
