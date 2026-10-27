// Separate test-number bridge. Run this script only when intentionally updating workflow 03.
import { writeFileSync } from 'node:fs';

const prepare = `const TEST_PHONE_ID = 'REPLACE_WITH_TEST_PHONE_NUMBER_ID';
const TEST_RECIPIENT = 'REPLACE_WITH_ALLOWLISTED_RECIPIENT';
const body = $input.first().json.body;
const valid = body && typeof body === 'object' && !Array.isArray(body)
  && typeof body.messageId === 'string' && body.messageId.length <= 200
  && body.messageId.startsWith('wamid.') && /^[\\x21-\\x7E]+$/.test(body.messageId)
  && body.phoneNumberId === TEST_PHONE_ID && body.senderWaId === TEST_RECIPIENT
  && typeof body.text === 'string' && body.text.trim().length > 0 && body.text.length <= 1000;
if (!valid) return [{json:{valid:false,error:'invalid_test_message'}}];
return [{json:{valid:true,messageId:body.messageId,recipient:TEST_RECIPIENT,
  aiRequest:{messageId:'meta_'+body.messageId.replace(/[^A-Za-z0-9_-]/g,'_').slice(0,75),
    customerId:'mock-meta-'+TEST_RECIPIENT,text:body.text.trim()}}}];`;

const prepareReply = `const inbound = $('Validate test message').first().json;
const response = $input.first().json;
const status = Number(response.statusCode ?? response.status ?? 0);
const ai = response.body ?? response;
if (status !== 200 || ai.ok !== true || typeof ai.reply !== 'string' || ai.reply.length < 1 || ai.reply.length > 1000)
  return [{json:{sendable:false,messageId:inbound.messageId,
    error:status === 429 || ai.error === 'ai_rate_limited' ? 'ai_rate_limited' : 'ai_unavailable',
    upstreamStatus:status,httpStatus:503}}];
return [{json:{sendable:true,messageId:inbound.messageId,
  outbound:{messaging_product:'whatsapp',recipient_type:'individual',to:inbound.recipient,type:'text',
    text:{preview_url:false,body:ai.reply}}}}];`;

const formatDelivery = `const inbound = $('Prepare test reply').first().json;
const response = $input.first().json;
const status = Number(response.statusCode ?? response.status ?? 0);
const graph = response.body ?? response;
const sentId = graph && Array.isArray(graph.messages) && graph.messages[0] && graph.messages[0].id;
const code = Number(graph?.error?.code ?? 0);
const accepted = status >= 200 && status < 300 && typeof sentId === 'string' && sentId.startsWith('wamid.');
return [{json:{ok:accepted,mode:'meta-test',messageId:inbound.messageId,
  ...(accepted ? {outboundMessageId:sentId} : {error:status === 429 || code === 4 || code === 17 || code === 80007
    ? 'meta_rate_limited' : code === 190 || status === 401 ? 'meta_token_expired' : 'meta_delivery_failed'}),
  httpStatus:accepted ? 200 : 503}}];`;

const credential = (id,name,type) => ({[type]:{id,name}});
const respond = (id,name,position,body,status) => ({id,name,type:'n8n-nodes-base.respondToWebhook',typeVersion:1.4,position,
  parameters:{respondWith:'json',responseBody:body,options:{responseCode:status,responseHeaders:{entries:[{name:'Cache-Control',value:'no-store'}]}}}});
const branch = (id,name,position,field) => ({id,name,type:'n8n-nodes-base.if',typeVersion:2.2,position,
  parameters:{conditions:{options:{caseSensitive:true,leftValue:'',typeValidation:'strict',version:2},
    conditions:[{id:'is-true',leftValue:`={{ $json.${field} }}`,rightValue:'',operator:{type:'boolean',operation:'true',singleValue:true}}],
    combinator:'and'},options:{}}});
const node = (id,name,type,position,parameters,more={}) => ({id,name,type,typeVersion:type==='n8n-nodes-base.code'?2:type==='n8n-nodes-base.httpRequest'?4.3:2.1,position,parameters,...more});

export const metaWorkflow = {
  id:'restaurantMetaTest03',name:'03 · Meta WhatsApp test bridge',active:false,
  nodes:[
    node('webhook','Authenticated internal test message','n8n-nodes-base.webhook',[0,0],
      {authentication:'headerAuth',httpMethod:'POST',path:'restaurant-dev/meta-bridge',responseMode:'responseNode',options:{}},
      {webhookId:'restaurant-meta-test-bridge',credentials:credential('restaurantMetaBridge03','Restaurant Meta Bridge','httpHeaderAuth')}),
    node('prepare','Validate test message','n8n-nodes-base.code',[250,0],{mode:'runOnceForAllItems',jsCode:prepare}),
    branch('valid','Valid test message?',[500,0],'valid'),
    node('ai','Call existing AI conversation','n8n-nodes-base.httpRequest',[750,-160],{
      method:'POST',url:'https://restaurant-n8n-5i1u.onrender.com/webhook/restaurant-dev/ai-conversation',
      authentication:'genericCredentialType',genericAuthType:'httpHeaderAuth',sendBody:true,contentType:'json',
      specifyBody:'json',jsonBody:'={{ JSON.stringify($json.aiRequest) }}',
      options:{timeout:15000,response:{response:{fullResponse:true,neverError:true,responseFormat:'json'}}}},
      {credentials:credential('restaurantHeaderAuth01','Restaurant development webhook','httpHeaderAuth'),onError:'continueRegularOutput'}),
    node('reply','Prepare test reply','n8n-nodes-base.code',[1000,-160],{mode:'runOnceForAllItems',jsCode:prepareReply}),
    branch('sendable','AI reply available?',[1250,-160],'sendable'),
    node('meta','Send via Meta test number','n8n-nodes-base.httpRequest',[1500,-260],{
      method:'POST',url:'https://graph.facebook.com/REPLACE_WITH_GRAPH_VERSION/REPLACE_WITH_TEST_PHONE_NUMBER_ID/messages',
      authentication:'genericCredentialType',genericAuthType:'httpBearerAuth',sendBody:true,contentType:'json',
      specifyBody:'json',jsonBody:'={{ JSON.stringify($json.outbound) }}',
      options:{timeout:12000,response:{response:{fullResponse:true,neverError:true,responseFormat:'json'}}}},
      {credentials:credential('restaurantMetaBearer03','Restaurant Meta Test API','httpBearerAuth'),onError:'continueRegularOutput'}),
    node('delivery','Classify Meta delivery','n8n-nodes-base.code',[1750,-260],{mode:'runOnceForAllItems',jsCode:formatDelivery}),
    respond('complete','Report test delivery',[2000,-260],
      '={{ {ok:$json.ok,mode:$json.mode,messageId:$json.messageId,...($json.outboundMessageId?{outboundMessageId:$json.outboundMessageId}:{}),...($json.error?{error:$json.error}:{})} }}',
      '={{ $json.httpStatus }}'),
    respond('invalid','Reject internal test message',[750,160],'{"ok":false,"error":"invalid_test_message"}',422),
    respond('ai-failed','Report AI unavailable',[1500,20],
      '={{ {ok:false,messageId:$json.messageId,error:$json.error,upstreamStatus:$json.upstreamStatus} }}','={{ $json.httpStatus }}'),
    node('note','Milestone 3 test contract','n8n-nodes-base.stickyNote',[0,-500],{
      content:'## Meta test bridge — inactive until configured\nOnly the ASP.NET Core signed webhook should feed this authenticated internal endpoint. Replace the test phone ID, allowlisted recipient and supported Graph version before importing. Bind separate Header Auth and Meta Bearer Auth credentials. This calls existing workflow 02 without changing it. It sends only to the one test recipient; no orders or payments.',height:245,width:850})
  ],
  connections:{
    'Authenticated internal test message':{main:[[{node:'Validate test message',type:'main',index:0}]]},
    'Validate test message':{main:[[{node:'Valid test message?',type:'main',index:0}]]},
    'Valid test message?':{main:[[{node:'Call existing AI conversation',type:'main',index:0}],[{node:'Reject internal test message',type:'main',index:0}]]},
    'Call existing AI conversation':{main:[[{node:'Prepare test reply',type:'main',index:0}]]},
    'Prepare test reply':{main:[[{node:'AI reply available?',type:'main',index:0}]]},
    'AI reply available?':{main:[[{node:'Send via Meta test number',type:'main',index:0}],[{node:'Report AI unavailable',type:'main',index:0}]]},
    'Send via Meta test number':{main:[[{node:'Classify Meta delivery',type:'main',index:0}]]},
    'Classify Meta delivery':{main:[[{node:'Report test delivery',type:'main',index:0}]]}
  },
  settings:{executionOrder:'v1',saveDataSuccessExecution:'none',saveDataErrorExecution:'none',saveManualExecutions:false},
  pinData:{},tags:[]
};

if (process.argv[1]?.endsWith('/build-meta-workflow.mjs')) {
  writeFileSync(new URL('../workflows/03-meta-test-bridge.json',import.meta.url),JSON.stringify(metaWorkflow,null,2)+'\n');
}
