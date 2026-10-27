// Read-only workflow check plus a few simulated Groq calls. No imports or workflow edits.
import assert from 'node:assert/strict';
import { loadPrivateEnv, ready, request } from './lib.mjs';

const base=(process.env.N8N_BASE_URL??'').replace(/\/$/,'');
const index=process.argv.indexOf('--env-file');
if(index<0||!process.argv[index+1]) throw new Error('Pass --env-file with the ignored private token file.');
const token=loadPrivateEnv(process.argv[index+1]).RESTAURANT_WEBHOOK_TOKEN;
if(!/^https:\/\//.test(base)||!token) throw new Error('Set HTTPS N8N_BASE_URL and a private webhook token.');
await ready(base,300000);
const original=await request(base,{messageId:'ai-remote-original',customerId:'mock-remote',text:'Hello'},token,'POST',60000);
assert.equal(original.status,200,'Original development webhook must remain published.');

async function send(text,messageId,auth=token){
  const headers={'Content-Type':'application/json'};
  if(auth) headers['X-Restaurant-Webhook-Token']=auth;
  const response=await fetch(base+'/webhook/restaurant-dev/ai-conversation',{
    method:'POST',headers,body:JSON.stringify({messageId,customerId:'mock-remote',text}),
    signal:AbortSignal.timeout(60000)
  });
  let data;try{data=await response.json();}catch{data={};}
  return {status:response.status,data};
}
const denied=await send('Hello','ai-remote-denied',null);
assert.ok([401,403].includes(denied.status),'AI webhook must reject missing authentication.');
const greeting=await send('Hello','ai-remote-greeting');
assert.equal(greeting.status,200,'Groq greeting call must succeed.');
assert.equal(greeting.data.intent,'greeting');
assert.equal(greeting.data.mode,'development');
const menu=await send('Show me the menu','ai-remote-menu');
assert.equal(menu.status,200,'Groq menu call must succeed.');
assert.equal(menu.data.intent,'menu');
assert.ok(menu.data.reply?.includes('GHS 45.00'));
const order=await send('Two jollof rice with chicken and one Coke','ai-remote-order');
assert.equal(order.status,200,'Groq mock-order call must succeed.');
assert.equal(order.data.intent,'order');
assert.equal(order.data.totalMinor,9800);
assert.match(order.data.reply,/Nothing has been placed or charged/);
console.log('Hosted original webhook, AI authentication, Groq greeting, menu and mock-order checks passed.');
