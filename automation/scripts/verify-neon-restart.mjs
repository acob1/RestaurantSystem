// Non-destructive check of the dedicated development instance; does not reset Neon or volumes.
import assert from 'node:assert/strict';
import { readFileSync, mkdirSync, writeFileSync } from 'node:fs';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { docker, loadPrivateEnv, ready, request, root } from './lib.mjs';
const env=loadPrivateEnv(new URL('../.env.neon',import.meta.url));
assert.equal(env['COMPOSE_PROJECT_NAME'],'restaurant-automation-neon');
assert.equal(env['DB_POSTGRESDB_DATABASE'],'restaurant_automation_dev');
assert.notEqual(env['AUTOMATION_PORT'],'5678');
const base=`http://127.0.0.1:${env['AUTOMATION_PORT']}`;
const args=['compose','--env-file','automation/.env.neon','-f','automation/compose.yml'];
const originalId=()=>docker(['ps','--filter','name=^restaurant-automation-n8n-1$','--format','{{.ID}}']);
const localBefore=originalId();
const body={...JSON.parse(readFileSync(new URL('../tests/fixtures/valid-message.json',import.meta.url))),messageId:'neon-persistence-001'};
await ready(base);
let start=Date.now();
const before=await request(base,body,env['RESTAURANT_WEBHOOK_TOKEN'],'POST',60000);
const beforeMs=Date.now()-start;
assert.equal(before.status,200);assert.equal(before.data.ok,true);assert.equal(before.data.mode,'development');
console.log('PASS Neon-backed authenticated webhook before container recreation');
docker([...args,'up','--detach','--no-deps','--force-recreate','n8n'],{env});
await ready(base,600000);
start=Date.now();
const after=await request(base,body,env['RESTAURANT_WEBHOOK_TOKEN'],'POST',60000);
const afterMs=Date.now()-start;
assert.equal(after.status,200);assert.deepEqual(after.data,before.data);
console.log('PASS Published workflow and protected credential reload after container recreation');
assert.equal(originalId(),localBefore,'The original local n8n container changed');
console.log('PASS Original local n8n container retained');
const check=spawnSync(process.execPath,[fileURLToPath(new URL('./check-neon.mjs',import.meta.url)),'--workflow-state'],{encoding:'utf8'});
if(check.error) throw check.error;
if(check.status!==0) {process.stderr.write(check.stderr);process.exit(check.status??1);}
const state=JSON.parse(check.stdout);assert.equal(state.bootstrapRecordCount,2);
console.log('PASS Neon TLS, schema permissions, workflow and credential records');
mkdirSync(new URL('artifacts/automation/',root),{recursive:true});
writeFileSync(new URL('artifacts/automation/neon-verification.json',root),JSON.stringify({
  executedAt:new Date().toISOString(),database:state.database,role:state.role,certificateVerified:state.certificateVerified,
  clientTlsVersion:state.clientTlsVersion,workflowAndCredentialRecords:state.bootstrapRecordCount,
  webhookBeforeMs:beforeMs,webhookAfterRecreationMs:afterMs,requestTimeoutMs:60000,
  originalLocalContainerRetained:true,containerRecreationPassed:true,renderDeploymentTested:false
},null,2)+'\n');
console.log('All 4 Neon verification checks passed. No database or volume was reset.');
