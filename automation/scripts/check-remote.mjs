// Read-only health plus one simulated authenticated webhook call. No import/publish writes.
import assert from 'node:assert/strict';
import { ready, request } from './lib.mjs';
const base=(process.env['N8N_BASE_URL']??'').replace(/\/$/,'');
const token=process.env['RESTAURANT_WEBHOOK_TOKEN'];
if(!/^https:\/\//.test(base)||!token) throw new Error('Set HTTPS N8N_BASE_URL and RESTAURANT_WEBHOOK_TOKEN in your shell.');
await ready(base);
const result=await request(base,{messageId:'remote-smoke-001',customerId:'mock-smoke',text:'Hello'},token);
assert.equal(result.status,200);assert.equal(result.data.mode,'development');assert.equal(result.data.ok,true);
console.log('Remote n8n readiness and authenticated development webhook passed.');
