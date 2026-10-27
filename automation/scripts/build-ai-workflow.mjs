// Generates the importable milestone-02 workflow. No provider credentials are read here.
import { readFileSync, writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

export const mockMenu=JSON.parse(readFileSync(new URL('../ai/mock-menu.json',import.meta.url),'utf8'));
const systemPrompt=readFileSync(new URL('../ai/system-prompt.txt',import.meta.url),'utf8').trim();
const prepareCode=`const MENU = ${JSON.stringify(mockMenu)};
const SYSTEM_PROMPT = ${JSON.stringify(systemPrompt)};
const incoming = $input.first().json;
const body = incoming.body;
const valid = body && typeof body === 'object' && !Array.isArray(body)
  && typeof body.messageId === 'string' && /^[A-Za-z0-9_-]{1,80}$/.test(body.messageId)
  && typeof body.customerId === 'string' && /^mock-[A-Za-z0-9_-]{1,64}$/.test(body.customerId)
  && typeof body.text === 'string' && body.text.trim().length > 0 && body.text.length <= 1000;
if (!valid) return [{json:{valid:false,error:'invalid_message'}}];
const prompt = SYSTEM_PROMPT + '\\nMock menu: ' + MENU.map(item => item.name + ' (SKU ' + item.sku + ', GHS ' + (item.priceMinor / 100).toFixed(2) + ')').join('; ');
const groqRequest={
  model:'openai/gpt-oss-20b',
  temperature:0.1,
  max_completion_tokens:220,
  stream:false,
  response_format:{type:'json_schema',json_schema:{name:'restaurant_intent',strict:true,schema:{
    type:'object',properties:{
      intent:{type:'string',enum:['greeting','menu','order','fallback']},
      items:{type:'array',items:{type:'object',properties:{sku:{type:'string',enum:MENU.map(item=>item.sku)},quantity:{type:'integer'}},required:['sku','quantity'],additionalProperties:false}}
    },required:['intent','items'],additionalProperties:false
  }}},
  messages:[{role:'system',content:prompt},{role:'user',content:body.text.trim()}]
};
return [{json:{valid:true,messageId:body.messageId,customerId:body.customerId,groqRequest}}];`;

const formatCode=`const MENU = ${JSON.stringify(mockMenu)};
const original = $('Prepare AI request').first().json;
const response = $input.first().json;
const base = {mode:'development',messageId:original.messageId,customerId:original.customerId,currency:'GHS',items:[],totalMinor:0};
const unavailable = (reason) => [{json:{...base,ok:false,error:reason,intent:'fallback',reply:'The demo assistant is temporarily unavailable. Please try again later.',httpStatus:503}}];
if (!response || response.error) return unavailable('ai_unavailable');
if (Number(response.statusCode) !== 200) return unavailable(Number(response.statusCode) === 429 ? 'ai_rate_limited' : 'ai_unavailable');
let choice;
try {
  const content = response.body?.choices?.[0]?.message?.content;
  if (typeof content !== 'string' || content.length > 3000) throw new Error('invalid content');
  choice = JSON.parse(content);
} catch { return unavailable('ai_invalid_response'); }
if (!choice || !['greeting','menu','order','fallback'].includes(choice.intent) || !Array.isArray(choice.items)) return unavailable('ai_invalid_response');
if (choice.intent === 'greeting') return [{json:{...base,ok:true,intent:'greeting',reply:'Akwaaba! Welcome to the Accra Kitchen demo. Ask to see our mock menu or tell me what you would like to try.',httpStatus:200}}];
if (choice.intent === 'menu') {
  const lines = MENU.map(item => item.name + ' — GHS ' + (item.priceMinor / 100).toFixed(2));
  return [{json:{...base,ok:true,intent:'menu',reply:'Mock menu:\\n' + lines.join('\\n') + '\\nThese are demo prices; no real order can be placed.',httpStatus:200}}];
}
if (choice.intent === 'order') {
  if (!choice.items.length || choice.items.length > 6) return [{json:{...base,ok:true,intent:'fallback',reply:'Please name up to six items from the mock menu and their quantities.',httpStatus:200}}];
  const seen = new Set();
  const items=[];
  for (const selection of choice.items) {
    const item = MENU.find(entry => entry.sku === selection.sku);
    if (!item || !Number.isInteger(selection.quantity) || selection.quantity < 1 || selection.quantity > 10 || seen.has(selection.sku)) {
      return [{json:{...base,ok:true,intent:'fallback',reply:'I could not match that request to the mock menu. Please ask for the menu and try again.',httpStatus:200}}];
    }
    seen.add(selection.sku);
    items.push({sku:item.sku,name:item.name,quantity:selection.quantity,unitPriceMinor:item.priceMinor,lineTotalMinor:item.priceMinor * selection.quantity});
  }
  const totalMinor=items.reduce((sum,item)=>sum+item.lineTotalMinor,0);
  const summary=items.map(item=>item.quantity + ' × ' + item.name).join(', ');
  return [{json:{...base,ok:true,intent:'order',items,totalMinor,reply:'Mock summary: ' + summary + '. Total GHS ' + (totalMinor / 100).toFixed(2) + '. Nothing has been placed or charged.',httpStatus:200}}];
}
return [{json:{...base,ok:true,intent:'fallback',reply:'I can show the mock menu or summarize a simple food order. What would you like?',httpStatus:200}}];`;

export const aiWorkflow={
  id:'restaurantAiConversation02',name:'02 · Restaurant AI conversation (development)',active:false,
  nodes:[
    {id:'ai-inbox',name:'AI development conversation',type:'n8n-nodes-base.webhook',typeVersion:2.1,position:[0,0],webhookId:'restaurant-ai-conversation',parameters:{authentication:'headerAuth',httpMethod:'POST',path:'restaurant-dev/ai-conversation',responseMode:'responseNode',options:{}},credentials:{httpHeaderAuth:{id:'restaurantHeaderAuth01',name:'Restaurant development webhook'}}},
    {id:'prepare',name:'Prepare AI request',type:'n8n-nodes-base.code',typeVersion:2,position:[250,0],parameters:{mode:'runOnceForAllItems',jsCode:prepareCode}},
    {id:'valid',name:'Valid AI input?',type:'n8n-nodes-base.if',typeVersion:2.2,position:[500,0],parameters:{conditions:{options:{caseSensitive:true,leftValue:'',typeValidation:'strict',version:2},conditions:[{id:'is-valid',leftValue:'={{ $json.valid }}',rightValue:'',operator:{type:'boolean',operation:'true',singleValue:true}}],combinator:'and'},options:{}}},
    {id:'groq',name:'Classify with Groq',type:'n8n-nodes-base.httpRequest',typeVersion:4.3,position:[750,-100],parameters:{method:'POST',url:'https://api.groq.com/openai/v1/chat/completions',authentication:'genericCredentialType',genericAuthType:'httpBearerAuth',sendBody:true,contentType:'json',specifyBody:'json',jsonBody:'={{ JSON.stringify($json.groqRequest) }}',options:{timeout:12000,response:{response:{fullResponse:true,neverError:true,responseFormat:'json'}}}},credentials:{httpBearerAuth:{id:'restaurantGroqBearer02',name:'Restaurant Groq API'}},onError:'continueRegularOutput'},
    {id:'format',name:'Validate AI result and format reply',type:'n8n-nodes-base.code',typeVersion:2,position:[1000,-100],parameters:{mode:'runOnceForAllItems',jsCode:formatCode}},
    {id:'ai-reply',name:'Reply to development conversation',type:'n8n-nodes-base.respondToWebhook',typeVersion:1.4,position:[1250,-100],parameters:{respondWith:'json',responseBody:'={{ { ok:$json.ok, mode:$json.mode, messageId:$json.messageId, customerId:$json.customerId, intent:$json.intent, reply:$json.reply, currency:$json.currency, items:$json.items, totalMinor:$json.totalMinor, ...($json.error ? {error:$json.error} : {}) } }}',options:{responseCode:'={{ $json.httpStatus }}',responseHeaders:{entries:[{name:'Cache-Control',value:'no-store'}]}}}},
    {id:'invalid',name:'Reject invalid AI input',type:'n8n-nodes-base.respondToWebhook',typeVersion:1.4,position:[750,150],parameters:{respondWith:'json',responseBody:'{"ok":false,"error":"invalid_message","message":"Provide messageId, a mock- customerId, and text of 1–1000 characters."}',options:{responseCode:422,responseHeaders:{entries:[{name:'Cache-Control',value:'no-store'}]}}}},
    {id:'notes',name:'Milestone 2 contract',type:'n8n-nodes-base.stickyNote',typeVersion:1,position:[-20,-360],parameters:{content:'## Mock AI conversation — development only\nPOST /webhook/restaurant-dev/ai-conversation\nUses the existing Header Auth token and a separate Groq Bearer Auth credential.\nGroq returns a constrained intent and SKU list; the workflow validates it and computes mock prices locally.\nNo cart persistence, order creation, payment, or WhatsApp delivery. Keep workflow 01 unchanged.',height:245,width:760}}
  ],
  connections:{
    'AI development conversation':{main:[[{node:'Prepare AI request',type:'main',index:0}]]},
    'Prepare AI request':{main:[[{node:'Valid AI input?',type:'main',index:0}]]},
    'Valid AI input?':{main:[[{node:'Classify with Groq',type:'main',index:0}],[{node:'Reject invalid AI input',type:'main',index:0}]]},
    'Classify with Groq':{main:[[{node:'Validate AI result and format reply',type:'main',index:0}]]},
    'Validate AI result and format reply':{main:[[{node:'Reply to development conversation',type:'main',index:0}]]}
  },
  settings:{executionOrder:'v1',executionTimeout:45,saveDataSuccessExecution:'none',saveDataErrorExecution:'none',saveExecutionProgress:false,saveManualExecutions:false},pinData:{},tags:[]
};

if(process.argv[1]===fileURLToPath(import.meta.url)){
  writeFileSync(new URL('../workflows/02-ai-conversation.json',import.meta.url),JSON.stringify(aiWorkflow,null,2)+'\n');
  console.log('Wrote the importable development AI workflow.');
}
