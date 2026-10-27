// Disposable n8n/PostgreSQL test with a local Groq stub. Never reads Neon or real Groq secrets.
import assert from 'node:assert/strict';
import { randomBytes } from 'node:crypto';
import { docker, ready, root, credential } from './lib.mjs';
import { aiWorkflow } from './build-ai-workflow.mjs';

const suffix = randomBytes(5).toString('hex');
const prefix = `restaurant-ai-test-${suffix}`;
const network = `${prefix}-net`, db = `${prefix}-db`, server = `${prefix}-app`, mock = `${prefix}-groq`, volume = `${prefix}-home`;
const image = 'restaurant-n8n:milestone02';
const password = randomBytes(24).toString('hex');
const webhookToken = randomBytes(32).toString('hex');
const groqToken = randomBytes(32).toString('hex');
const env = { DB_TYPE: 'postgresdb', DB_POSTGRESDB_HOST: db, DB_POSTGRESDB_PORT: '5432', DB_POSTGRESDB_DATABASE: 'restaurant_automation_test', DB_POSTGRESDB_USER: 'n8n_test', DB_POSTGRESDB_PASSWORD: password, DB_POSTGRESDB_SSL_ENABLED: 'false', DB_POSTGRESDB_POOL_SIZE: '2', N8N_ENCRYPTION_KEY: randomBytes(32).toString('hex'), N8N_HOST: 'localhost', N8N_PORT: '5678', N8N_PROTOCOL: 'http', N8N_SECURE_COOKIE: 'false' };
const envArgs = Object.keys(env).flatMap(key => ['--env', key]);
let networkCreated = false, dbCreated = false, mockCreated = false, volumeCreated = false, serverCreated = false;
const passed = [];
function pass(name, condition) { assert.ok(condition, name); passed.push(name); console.log('PASS ' + name); }
async function post(base, path, body, token) {
  const headers = { 'Content-Type': 'application/json' };
  if (token) headers['X-Restaurant-Webhook-Token'] = token;
  const response = await fetch(base + path, { method: 'POST', headers, body: JSON.stringify(body), signal: AbortSignal.timeout(45000) });
  let data; try { data = await response.json(); } catch { data = {}; }
  return { status: response.status, data };
}
try {
  docker(['build', '--tag', image, 'automation'], { quiet: false });
  docker(['network', 'create', network]); networkCreated = true;
  docker(['volume', 'create', volume]); volumeCreated = true;
  docker(['run', '--detach', '--name', db, '--network', network, '--env', 'POSTGRES_DB=restaurant_automation_test', '--env', 'POSTGRES_USER=n8n_test', '--env', 'POSTGRES_PASSWORD', 'postgres:17.6-alpine'], { env: { POSTGRES_PASSWORD: password } }); dbCreated = true;
  let databaseReady = false;
  for (let i = 0; i < 60; i++) {
    try { docker(['exec', db, 'pg_isready', '-U', 'n8n_test', '-d', 'restaurant_automation_test']); databaseReady = true; break; } catch { await new Promise(resolve => setTimeout(resolve, 1000)); }
  }
  pass('disposable PostgreSQL ready', databaseReady);
  docker(['run', '--detach', '--name', mock, '--network', network, '--env', 'MOCK_GROQ_TOKEN', '--volume', `${root.pathname}automation/tests/mock-groq-server.cjs:/tmp/mock-groq-server.cjs:ro`, '--entrypoint', 'node', image, '/tmp/mock-groq-server.cjs'], { env: { MOCK_GROQ_TOKEN: groqToken } }); mockCreated = true;
  docker(['run', '--detach', '--name', server, '--network', network, '--memory', '512m', '--memory-swap', '512m', '--publish', '127.0.0.1::5678', '--volume', `${volume}:/home/node/.n8n`, ...envArgs, image], { env }); serverCreated = true;
  let base = `http://${docker(['port', server, '5678/tcp']).split('\n')[0]}`;
  await ready(base, 300000);
  const setup = await fetch(base + '/rest/owner/setup', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ email: 'ai-tests@example.invalid', firstName: 'AI', lastName: 'Tests', password: 'Aa9!' + randomBytes(20).toString('hex') }), signal: AbortSignal.timeout(15000) });
  pass('disposable n8n owner setup', setup.ok);
  docker(['stop', '--time', '10', server]);
  const cli = ['run', '--rm', '--network', network, '--memory', '512m', '--memory-swap', '512m', '--volume', `${volume}:/home/node/.n8n`, ...envArgs];
  const credentials = [...credential(webhookToken), { id: 'restaurantGroqBearer02', name: 'Restaurant Groq API', type: 'httpBearerAuth', data: { token: groqToken } }];
  docker([...cli, '--interactive', '--entrypoint', '/bin/sh', image, '-c', 'umask 077; cat > /tmp/restaurant-ai-test-credentials.json; n8n import:credentials --input=/tmp/restaurant-ai-test-credentials.json; status=$?; rm -f /tmp/restaurant-ai-test-credentials.json; exit "$status"'], { env, input: JSON.stringify(credentials) });
  docker([...cli, '--entrypoint', 'n8n', image, 'import:workflow', '--input=/opt/restaurant/workflows/01-development-inbox.json'], { env });
  const disposable = structuredClone(aiWorkflow);
  disposable.nodes.find(node => node.name === 'Classify with Groq').parameters.url = 'http://' + mock + ':3000/v1/chat/completions';
  docker([...cli, '--interactive', '--entrypoint', '/bin/sh', image, '-c', 'umask 077; cat > /tmp/restaurant-ai-test-workflow.json; n8n import:workflow --input=/tmp/restaurant-ai-test-workflow.json; status=$?; rm -f /tmp/restaurant-ai-test-workflow.json; exit "$status"'], { env, input: JSON.stringify(disposable) });
  docker([...cli, '--entrypoint', 'n8n', image, 'publish:workflow', '--id=restaurantWebhook01'], { env });
  docker([...cli, '--entrypoint', 'n8n', image, 'publish:workflow', '--id=restaurantAiConversation02'], { env });
  const published = docker(['exec', db, 'psql', '-U', 'n8n_test', '-d', 'restaurant_automation_test', '-Atc', "SELECT id || ':' || active FROM workflow_entity WHERE id IN ('restaurantWebhook01','restaurantAiConversation02') ORDER BY id"]);
  console.log('Disposable workflow states:', published.replaceAll('restaurantWebhook01', 'first').replaceAll('restaurantAiConversation02', 'second'));
  docker(['start', server]);
  base = `http://${docker(['port', server, '5678/tcp']).split('\n')[0]}`;
  await ready(base, 300000);
  let original;
  for (let i = 0; i < 20; i++) {
    original = await post(base, '/webhook/restaurant-dev/inbox', { messageId: 'old-flow', customerId: 'mock-guest', text: 'Hello' }, webhookToken);
    if (original.status !== 404) break;
    await new Promise(resolve => setTimeout(resolve, 1000));
  }
  if (original.status !== 200) console.log('Original workflow diagnostic status:', original.status, 'error:', original.data.error ?? original.data.code ?? 'none');
  pass('original development webhook still works', original.status === 200 && original.data.mode === 'development');
  const missingAuth = await post(base, '/webhook/restaurant-dev/ai-conversation', { messageId: 'm1', customerId: 'mock-guest', text: 'Hello' });
  pass('AI webhook requires authentication', [401, 403].includes(missingAuth.status));
  const invalid = await post(base, '/webhook/restaurant-dev/ai-conversation', { messageId: 'm2', customerId: 'real-user', text: 'Hello' }, webhookToken);
  pass('AI webhook rejects non-mock customers', invalid.status === 422 && invalid.data.error === 'invalid_message');
  const greeting = await post(base, '/webhook/restaurant-dev/ai-conversation', { messageId: 'm3', customerId: 'mock-guest', text: 'Hello' }, webhookToken);
  pass('Groq stub classifies greeting', greeting.status === 200 && greeting.data.intent === 'greeting' && /Akwaaba/.test(greeting.data.reply));
  const menu = await post(base, '/webhook/restaurant-dev/ai-conversation', { messageId: 'm4', customerId: 'mock-guest', text: 'Show me the menu' }, webhookToken);
  pass('AI menu uses fixed mock prices', menu.status === 200 && menu.data.intent === 'menu' && menu.data.reply.includes('GHS 45.00'));
  const order = await post(base, '/webhook/restaurant-dev/ai-conversation', { messageId: 'm5', customerId: 'mock-guest', text: 'Two jollof rice with chicken and one Coke' }, webhookToken);
  pass('basic order is only a priced mock summary', order.status === 200 && order.data.intent === 'order' && order.data.totalMinor === 9800 && /Nothing has been placed/.test(order.data.reply));
  const fallback = await post(base, '/webhook/restaurant-dev/ai-conversation', { messageId: 'm6', customerId: 'mock-guest', text: 'What time is it?' }, webhookToken);
  pass('fallback is safe', fallback.status === 200 && fallback.data.intent === 'fallback');
  const limited = await post(base, '/webhook/restaurant-dev/ai-conversation', { messageId: 'm7', customerId: 'mock-guest', text: 'rate-limit' }, webhookToken);
  pass('Groq rate limit returns a safe 503', limited.status === 503 && limited.data.error === 'ai_rate_limited');
  const malformed = await post(base, '/webhook/restaurant-dev/ai-conversation', { messageId: 'm8', customerId: 'mock-guest', text: 'bad-json' }, webhookToken);
  pass('malformed Groq response returns a safe 503', malformed.status === 503 && malformed.data.error === 'ai_invalid_response');
  const unknown = await post(base, '/webhook/restaurant-dev/ai-conversation', { messageId: 'm9', customerId: 'mock-guest', text: 'unknown-item' }, webhookToken);
  pass('invented menu SKU is rejected', unknown.status === 200 && unknown.data.intent === 'fallback' && unknown.data.totalMinor === 0);
  const timedOut = await post(base, '/webhook/restaurant-dev/ai-conversation', { messageId: 'm10', customerId: 'mock-guest', text: 'timeout' }, webhookToken);
  pass('Groq timeout returns a safe 503', timedOut.status === 503 && timedOut.data.error === 'ai_unavailable');
  const stored = docker(['exec', db, 'psql', '-U', 'n8n_test', '-d', 'restaurant_automation_test', '-Atc', "SELECT data FROM credentials_entity WHERE id='restaurantGroqBearer02'"]);
  pass('disposable Groq credential is encrypted', stored.length > 0 && !stored.includes(groqToken));
  console.log(`All ${passed.length} disposable AI runtime checks passed.`);
} finally {
  if (serverCreated) docker(['rm', '--force', server]);
  if (mockCreated) docker(['rm', '--force', mock]);
  if (dbCreated) docker(['rm', '--force', db]);
  if (volumeCreated) docker(['volume', 'rm', volume]);
  if (networkCreated) docker(['network', 'rm', network]);
}
