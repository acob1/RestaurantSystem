// Disposable n8n/PostgreSQL/Groq/Graph test. No Meta or Groq account is contacted.
import assert from 'node:assert/strict';
import {randomBytes} from 'node:crypto';
import {docker,ready,root,credential} from './lib.mjs';
import {aiWorkflow} from './build-ai-workflow.mjs';
import {metaWorkflow} from './build-meta-workflow.mjs';

const suffix=randomBytes(5).toString('hex');
const prefix=`restaurant-meta-test-${suffix}`;
const network=`${prefix}-net`,db=`${prefix}-db`,server=`${prefix}-app`,groq=`${prefix}-groq`,meta=`${prefix}-meta`,volume=`${prefix}-home`;
const image='restaurant-n8n:milestone03';
const password=randomBytes(24).toString('hex');
const webhookToken=randomBytes(32).toString('hex');
const bridgeToken=randomBytes(32).toString('hex');
const groqToken=randomBytes(32).toString('hex');
const metaToken=randomBytes(32).toString('hex');
const env={DB_TYPE:'postgresdb',DB_POSTGRESDB_HOST:db,DB_POSTGRESDB_PORT:'5432',
  DB_POSTGRESDB_DATABASE:'restaurant_automation_test',DB_POSTGRESDB_USER:'n8n_test',
  DB_POSTGRESDB_PASSWORD:password,DB_POSTGRESDB_SSL_ENABLED:'false',DB_POSTGRESDB_POOL_SIZE:'2',
  N8N_ENCRYPTION_KEY:randomBytes(32).toString('hex'),N8N_HOST:'localhost',N8N_PORT:'5678',
  N8N_PROTOCOL:'http',N8N_SECURE_COOKIE:'false'};
const envArgs=Object.keys(env).flatMap(key=>['--env',key]);
const made=[];const passed=[];
function pass(name,condition){assert.ok(condition,name);passed.push(name);console.log('PASS '+name);}
async function post(base,body,token){
  const headers={'Content-Type':'application/json'};
  if(token) headers['X-Restaurant-Meta-Bridge-Token']=token;
  const response=await fetch(base+'/webhook/restaurant-dev/meta-bridge',{
    method:'POST',headers,body:JSON.stringify(body),signal:AbortSignal.timeout(45000)});
  let data;try{data=await response.json();}catch{data={};}
  return {status:response.status,data};
}
function importJson(cli,value,kind){
  docker([...cli,'--interactive','--entrypoint','/bin/sh',image,'-c',
    `umask 077; cat > /tmp/restaurant-import.json; n8n import:${kind} --input=/tmp/restaurant-import.json; status=$?; rm -f /tmp/restaurant-import.json; exit "$status"`],
    {env,input:JSON.stringify(value)});
}
try{
  docker(['build','--tag',image,'automation'],{quiet:false});
  docker(['network','create',network]);made.push(['network','rm',network]);
  docker(['volume','create',volume]);made.push(['volume','rm',volume]);
  docker(['run','--detach','--name',db,'--network',network,'--env','POSTGRES_DB=restaurant_automation_test',
    '--env','POSTGRES_USER=n8n_test','--env','POSTGRES_PASSWORD','postgres:17.6-alpine'],
    {env:{POSTGRES_PASSWORD:password}});made.push(['rm','--force',db]);
  let dbReady=false;
  for(let i=0;i<60;i++){
    try{docker(['exec',db,'pg_isready','-U','n8n_test','-d','restaurant_automation_test']);dbReady=true;break;}
    catch{await new Promise(resolve=>setTimeout(resolve,1000));}
  }
  pass('disposable PostgreSQL ready',dbReady);
  docker(['run','--detach','--name',groq,'--network',network,'--env','MOCK_GROQ_TOKEN',
    '--volume',`${root.pathname}automation/tests/mock-groq-server.cjs:/tmp/mock-groq-server.cjs:ro`,
    '--entrypoint','node',image,'/tmp/mock-groq-server.cjs'],{env:{MOCK_GROQ_TOKEN:groqToken}});
  made.push(['rm','--force',groq]);
  docker(['run','--detach','--name',meta,'--network',network,'--env','MOCK_META_TOKEN',
    '--volume',`${root.pathname}automation/tests/mock-meta-server.cjs:/tmp/mock-meta-server.cjs:ro`,
    '--entrypoint','node',image,'/tmp/mock-meta-server.cjs'],{env:{MOCK_META_TOKEN:metaToken}});
  made.push(['rm','--force',meta]);
  docker(['run','--detach','--name',server,'--network',network,'--memory','512m','--memory-swap','512m',
    '--publish','127.0.0.1::5678','--volume',`${volume}:/home/node/.n8n`,...envArgs,image],{env});
  made.push(['rm','--force',server]);
  let base=`http://${docker(['port',server,'5678/tcp']).split('\n')[0]}`;
  await ready(base,300000);
  const setup=await fetch(base+'/rest/owner/setup',{method:'POST',headers:{'Content-Type':'application/json'},
    body:JSON.stringify({email:'meta-tests@example.invalid',firstName:'Meta',lastName:'Tests',
      password:'Aa9!'+randomBytes(20).toString('hex')}),signal:AbortSignal.timeout(15000)});
  pass('disposable n8n owner setup',setup.ok);
  docker(['stop','--time','10',server]);
  const cli=['run','--rm','--network',network,'--memory','512m','--memory-swap','512m',
    '--volume',`${volume}:/home/node/.n8n`,...envArgs];
  importJson(cli,[...credential(webhookToken),
    {id:'restaurantGroqBearer02',name:'Restaurant Groq API',type:'httpBearerAuth',data:{token:groqToken}},
    {id:'restaurantMetaBridge03',name:'Restaurant Meta Bridge',type:'httpHeaderAuth',
      data:{name:'X-Restaurant-Meta-Bridge-Token',value:bridgeToken}},
    {id:'restaurantMetaBearer03',name:'Restaurant Meta Test API',type:'httpBearerAuth',data:{token:metaToken}}],
    'credentials');
  docker([...cli,'--entrypoint','n8n',image,'import:workflow','--input=/opt/restaurant/workflows/01-development-inbox.json'],{env});
  const ai=structuredClone(aiWorkflow);
  ai.nodes.find(n=>n.name==='Classify with Groq').parameters.url=`http://${groq}:3000/v1/chat/completions`;
  importJson(cli,ai,'workflow');
  const bridge=structuredClone(metaWorkflow);
  const check=bridge.nodes.find(n=>n.name==='Validate test message');
  check.parameters.jsCode=check.parameters.jsCode.replaceAll('REPLACE_WITH_TEST_PHONE_NUMBER_ID','123456789')
    .replaceAll('REPLACE_WITH_ALLOWLISTED_RECIPIENT','233201234567');
  bridge.nodes.find(n=>n.name==='Call existing AI conversation').parameters.url=`http://${server}:5678/webhook/restaurant-dev/ai-conversation`;
  bridge.nodes.find(n=>n.name==='Send via Meta test number').parameters.url=`http://${meta}:3001/v26.0/123456789/messages`;
  importJson(cli,bridge,'workflow');
  for(const id of ['restaurantWebhook01','restaurantAiConversation02','restaurantMetaTest03'])
    docker([...cli,'--entrypoint','n8n',image,'publish:workflow',`--id=${id}`],{env});
  const states=docker(['exec',db,'psql','-U','n8n_test','-d','restaurant_automation_test','-Atc',
    "SELECT count(*) FROM workflow_entity WHERE id IN ('restaurantWebhook01','restaurantAiConversation02','restaurantMetaTest03') AND active=true"]);
  pass('three independent workflows published in disposable n8n',states==='3');
  docker(['start',server]);
  base=`http://${docker(['port',server,'5678/tcp']).split('\n')[0]}`;
  await ready(base,300000);
  let aiDirect;
  for(let i=0;i<30;i++){
    aiDirect=await fetch(base+'/webhook/restaurant-dev/ai-conversation',{
      method:'POST',headers:{'Content-Type':'application/json','X-Restaurant-Webhook-Token':webhookToken},
      body:JSON.stringify({messageId:'direct-ai',customerId:'mock-test',text:'Hello'}),signal:AbortSignal.timeout(45000)});
    if(aiDirect.status!==404)break;
    await new Promise(resolve=>setTimeout(resolve,1000));
  }
  if(aiDirect.status!==200)console.log('Direct AI diagnostic status:',aiDirect.status);
  pass('existing AI workflow responds in disposable n8n',aiDirect.status===200);
  const message=(text,id)=>({messageId:`wamid.${id}`,phoneNumberId:'123456789',senderWaId:'233201234567',text});
  let result;
  for(let i=0;i<20;i++){
    result=await post(base,message('Hello','first'),bridgeToken);
    if(result.status!==404)break;
    await new Promise(resolve=>setTimeout(resolve,1000));
  }
  if(result.status!==200)console.log('Bridge diagnostic status:',result.status,result.data?.error??'none',result.data?.upstreamStatus??'none');
  pass('mock Graph accepts a greeting reply from existing AI workflow',result.status===200 && result.data.ok===true && result.data.outboundMessageId==='wamid.mock-outbound');
  const unauth=await post(base,message('Hello','unauth'));
  pass('bridge rejects missing Header Auth',[401,403].includes(unauth.status));
  const other=await post(base,{...message('Hello','other'),senderWaId:'233209999999'},bridgeToken);
  pass('bridge rejects a non-allowlisted recipient',other.status===422);
  const limited=await post(base,message('Show me the menu','limit'),bridgeToken);
  pass('mock Graph rate limit is reported safely',limited.status===503 && limited.data.error==='meta_rate_limited');
  const expired=await post(base,message('Two jollof and one Coke','expired'),bridgeToken);
  pass('mock Graph expired token is reported safely',expired.status===503 && expired.data.error==='meta_token_expired');
  const aiLimited=await post(base,message('rate-limit','ai-limited'),bridgeToken);
  pass('AI rate limit prevents any outbound request',aiLimited.status===503 && aiLimited.data.error==='ai_rate_limited');
  const stored=docker(['exec',db,'psql','-U','n8n_test','-d','restaurant_automation_test','-Atc',
    "SELECT data FROM credentials_entity WHERE id='restaurantMetaBearer03'"]);
  pass('mock Meta token is encrypted in n8n PostgreSQL',stored.length>0 && !stored.includes(metaToken));
  console.log(`All ${passed.length} disposable Meta bridge runtime checks passed.`);
}finally{
  for(const args of made.reverse())try{docker(args);}catch{ /* best-effort cleanup */ }
}
