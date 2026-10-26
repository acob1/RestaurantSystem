// Read-only health plus one simulated authenticated webhook call. No import/publish writes.
import assert from 'node:assert/strict';
import { loadPrivateEnv, ready, request } from './lib.mjs';
const base=(process.env['N8N_BASE_URL']??'').replace(/\/$/,'');
const envFileIndex=process.argv.indexOf('--env-file');
if(envFileIndex>=0&&!process.argv[envFileIndex+1]) throw new Error('Pass a path after --env-file.');
const privateEnv=envFileIndex>=0?loadPrivateEnv(process.argv[envFileIndex+1]):{};
const token=process.env['RESTAURANT_WEBHOOK_TOKEN']??privateEnv['RESTAURANT_WEBHOOK_TOKEN'];
if(!/^https:\/\//.test(base)||!token) throw new Error('Set HTTPS N8N_BASE_URL and provide RESTAURANT_WEBHOOK_TOKEN through your shell or --env-file.');
await ready(base,300000);
const result=await request(base,{messageId:'remote-smoke-001',customerId:'mock-smoke',text:'Hello'},token,'POST',60000);
assert.equal(result.status,200);assert.equal(result.data.mode,'development');assert.equal(result.data.ok,true);
console.log('Remote n8n readiness and authenticated development webhook passed.');
