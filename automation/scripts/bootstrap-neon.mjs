// Add only the first workflow/credential to the dedicated development Neon instance.
// Existing records are retained. No database/table is deleted or reset.
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { credential, docker, loadPrivateEnv, ready, workflow } from './lib.mjs';
const env=loadPrivateEnv(new URL('../.env.neon',import.meta.url));
assert.equal(env['COMPOSE_PROJECT_NAME'],'restaurant-automation-neon');
assert.notEqual(env['AUTOMATION_PORT'],'5678');
assert.ok(env['RESTAURANT_WEBHOOK_TOKEN']?.length>=32&&!env['RESTAURANT_WEBHOOK_TOKEN'].includes('REPLACE'));
const compose=['compose','--env-file','automation/.env.neon','-f','automation/compose.yml'];
const neonCompose=(args,options={})=>docker([...compose,...args],{...options,env});
assert.ok(neonCompose(['ps','--quiet','n8n']),'Start the Neon workspace first.');
const base=`http://127.0.0.1:${env['AUTOMATION_PORT']}`;
await ready(base,600000);
const check=spawnSync(process.execPath,[fileURLToPath(new URL('./check-neon.mjs',import.meta.url)),'--workflow-state'],{encoding:'utf8'});
if(check.error) throw check.error;
if(check.status!==0) {process.stderr.write(check.stderr);process.exit(check.status??1);}
const state=JSON.parse(check.stdout);
if(state.bootstrapRecordCount!==0) {
  console.log('Existing Neon restaurant workflow/credential retained. Use the editor for deliberate updates.');
  process.exit(0);
}
neonCompose(['stop','n8n']);
try {
  neonCompose(['run','--rm','--no-deps','-T','--entrypoint','/bin/sh','n8n','-c',
    'umask 077; cat > /tmp/restaurant-credentials.json; n8n import:credentials --input=/tmp/restaurant-credentials.json; status=$?; rm -f /tmp/restaurant-credentials.json; exit "$status"'],
    {input:JSON.stringify(credential(env['RESTAURANT_WEBHOOK_TOKEN']))});
  neonCompose(['run','--rm','--no-deps','-T','--entrypoint','n8n','n8n','import:workflow','--input=/opt/restaurant/workflows/01-development-inbox.json']);
  neonCompose(['run','--rm','--no-deps','-T','--entrypoint','n8n','n8n','publish:workflow',`--id=${workflow.id}`]);
} finally {
  neonCompose(['start','n8n']);
}
await ready(base,300000);
console.log('First development webhook published with its protected credential in Neon.');
