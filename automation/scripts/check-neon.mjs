// Read-only PostgreSQL/TLS/privilege check. Secrets are passed through the child environment.
import assert from 'node:assert/strict';
import { docker, loadPrivateEnv } from './lib.mjs';
const env=loadPrivateEnv(new URL('../.env.neon',import.meta.url));
assert.match(env['DB_POSTGRESDB_HOST']??'',/^[a-z0-9.-]+\.neon\.tech$/i,'Use the direct Neon hostname.');
assert.ok(!env['DB_POSTGRESDB_HOST'].includes('-pooler'),'Use a direct endpoint for n8n migrations.');
assert.equal(env['DB_POSTGRESDB_DATABASE'],'restaurant_automation_dev');
assert.equal(env['DB_POSTGRESDB_USER'],'restaurant_automation');
assert.equal(env['DB_POSTGRESDB_SSL_ENABLED'],'true');
assert.ok(env['DB_POSTGRESDB_PASSWORD']&&!/PASTE_|REPLACE/.test(env['DB_POSTGRESDB_PASSWORD']),'Enter the new database password in automation/.env.neon first.');
const names=['DB_POSTGRESDB_HOST','DB_POSTGRESDB_PORT','DB_POSTGRESDB_DATABASE','DB_POSTGRESDB_USER','DB_POSTGRESDB_PASSWORD'];
const wantsWorkflowState=process.argv.includes('--workflow-state');
const stateQuery="SELECT (SELECT count(*) FROM workflow_entity WHERE id='restaurantWebhook01') + (SELECT count(*) FROM credentials_entity WHERE id='restaurantHeaderAuth01') AS record_count";
const script=`
const assert=require('node:assert/strict');
const {Client}=require('/usr/local/lib/node_modules/n8n/node_modules/pg');
const e=process.env;
const client=new Client({host:e.DB_POSTGRESDB_HOST,port:Number(e.DB_POSTGRESDB_PORT||5432),
  database:e.DB_POSTGRESDB_DATABASE,user:e.DB_POSTGRESDB_USER,password:e.DB_POSTGRESDB_PASSWORD,
  ssl:{rejectUnauthorized:true},enableChannelBinding:true,connectionTimeoutMillis:20000,query_timeout:15000});
(async()=>{
  try {
    await client.connect();
    assert.equal(client.connection.stream.authorized,true,'TLS certificate verification failed');
    const {rows}=await client.query(\`SELECT current_database() AS database, current_user AS role,
      has_schema_privilege(current_user, 'public', 'USAGE') AS schema_usage,
      has_schema_privilege(current_user, 'public', 'CREATE') AS schema_create\`);
    // Verify the actual client TLS socket. Neon can terminate TLS at its proxy,
    // so pg_stat_ssl on the backend is not evidence about this client connection.
    assert.equal(client.connection.stream.encrypted,true,'Client transport must be TLS');
    assert.equal(rows.length,1);
    assert.equal(rows[0].schema_usage,true,'Schema USAGE permission');assert.equal(rows[0].schema_create,true,'Schema CREATE permission');
    const result={status:'passed',certificateVerified:true,clientTlsVersion:client.connection.stream.getProtocol(),...rows[0]};
    ${wantsWorkflowState ? `result.bootstrapRecordCount=Number((await client.query(${JSON.stringify(stateQuery)})).rows[0].record_count);` : ''}
    console.log(JSON.stringify(result));
  } catch(error) {
    const message=String(error.message).split(e.DB_POSTGRESDB_PASSWORD).join('[redacted]').slice(0,300);
    console.error('Neon read-only check failed ('+(error.code||'verification')+'): '+message);
    process.exitCode=1;
  } finally {await client.end();}
})();
`;
const result=docker(['run','--rm','--interactive','--memory','128m',
  ...names.flatMap(name=>['--env',name]),'--entrypoint','node','docker.n8n.io/n8nio/n8n:2.44.7','-'],{env,input:script});
console.log(result);
