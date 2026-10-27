import test from 'node:test';
import assert from 'node:assert/strict';
import vm from 'node:vm';
import {readFileSync} from 'node:fs';
import {metaWorkflow} from '../scripts/build-meta-workflow.mjs';

const exported=JSON.parse(readFileSync(new URL('../workflows/03-meta-test-bridge.json',import.meta.url),'utf8'));
const node=name=>exported.nodes.find(item=>item.name===name);
const input={messageId:'wamid.test123',phoneNumberId:'123456789',senderWaId:'233201234567',text:'Hello'};
function run(name,json,previous={}){
  let source=node(name).parameters.jsCode
    .replaceAll('REPLACE_WITH_TEST_PHONE_NUMBER_ID','123456789')
    .replaceAll('REPLACE_WITH_ALLOWLISTED_RECIPIENT','233201234567');
  const ctx={$input:{first:()=>({json})},$:(sourceName)=>({first:()=>({json:previous[sourceName]})})};
  const output=vm.runInNewContext('(function(){\n'+source+'\n})()',ctx,{timeout:1000});
  return JSON.parse(JSON.stringify(output[0].json));
}

test('third workflow is distinct, inactive and credential protected',()=>{
  assert.deepEqual(exported,metaWorkflow);
  assert.equal(exported.id,'restaurantMetaTest03');
  assert.equal(exported.active,false);
  assert.equal(node('Authenticated internal test message').parameters.authentication,'headerAuth');
  assert.equal(node('Authenticated internal test message').credentials.httpHeaderAuth.name,'Restaurant Meta Bridge');
  assert.equal(node('Call existing AI conversation').parameters.url,'https://restaurant-n8n-5i1u.onrender.com/webhook/restaurant-dev/ai-conversation');
  assert.equal(node('Call existing AI conversation').credentials.httpHeaderAuth.name,'Restaurant development webhook');
  assert.equal(node('Send via Meta test number').credentials.httpBearerAuth.name,'Restaurant Meta Test API');
  assert.ok(!JSON.stringify(exported).includes('gsk_'));
  assert.equal(exported.settings.saveDataSuccessExecution,'none');
});

test('all workflow connections resolve',()=>{
  const names=new Set(exported.nodes.map(item=>item.name));
  for(const [source,connection] of Object.entries(exported.connections)){
    assert.ok(names.has(source));
    for(const branch of connection.main) for(const target of branch) assert.ok(names.has(target.node));
  }
});

test('bridge accepts only one configured test number and sender',()=>{
  const prepared=run('Validate test message',{body:input});
  assert.equal(prepared.valid,true);
  assert.equal(prepared.aiRequest.customerId,'mock-meta-233201234567');
  assert.equal(prepared.aiRequest.messageId.includes('.'),false);
  assert.equal(prepared.aiRequest.text,'Hello');
  assert.equal(run('Validate test message',{body:{...input,messageId:'wamid.AB+/='}}).valid,true);
  for(const body of [{...input,senderWaId:'233209999999'},{...input,phoneNumberId:'0'},
    {...input,messageId:'wrong'}, {...input,text:''},{...input,text:'x'.repeat(1001)}]){
    assert.equal(run('Validate test message',{body}).valid,false);
  }
});

test('only a successful bounded AI reply produces a Meta text request',()=>{
  const prepared=run('Validate test message',{body:input});
  const valid=run('Prepare test reply',{statusCode:200,body:{ok:true,reply:'Akwaaba!'}},{'Validate test message':prepared});
  assert.equal(valid.sendable,true);
  assert.equal(valid.outbound.to,'233201234567');
  assert.equal(valid.outbound.text.body,'Akwaaba!');
  for(const response of [{statusCode:429,body:{}},{statusCode:200,body:{ok:false,reply:'bad'}},
    {statusCode:200,body:{ok:true,reply:'x'.repeat(1001)}}]){
    const bad=run('Prepare test reply',response,{'Validate test message':prepared});
    assert.equal(bad.sendable,false);
    assert.equal(bad.outbound,undefined);
  }
  const rateLimited=run('Prepare test reply',{statusCode:503,body:{ok:false,error:'ai_rate_limited'}},{'Validate test message':prepared});
  assert.equal(rateLimited.error,'ai_rate_limited');
});

test('Graph success and temporary-token/rate-limit failures are classified without secrets',()=>{
  const previous={'Prepare test reply':{messageId:input.messageId}};
  const sent=run('Classify Meta delivery',{statusCode:200,body:{messages:[{id:'wamid.sent'}]}},previous);
  assert.equal(sent.ok,true);
  assert.equal(sent.outboundMessageId,'wamid.sent');
  const expired=run('Classify Meta delivery',{statusCode:401,body:{error:{code:190,message:'secret data'}}},previous);
  assert.equal(expired.error,'meta_token_expired');
  assert.equal(JSON.stringify(expired).includes('secret data'),false);
  const limited=run('Classify Meta delivery',{statusCode:429,body:{error:{code:4}}},previous);
  assert.equal(limited.error,'meta_rate_limited');
  const failed=run('Classify Meta delivery',{statusCode:400,body:{error:{code:100}}},previous);
  assert.equal(failed.error,'meta_delivery_failed');
});
