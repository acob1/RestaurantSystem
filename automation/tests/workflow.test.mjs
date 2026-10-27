import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { workflow } from '../scripts/lib.mjs';
test('the first development workflow remains independently importable',()=>{
  assert.equal(workflow.id,'restaurantWebhook01');assert.equal(workflow.active,false);
  const hook=workflow.nodes.find(n=>n.type==='n8n-nodes-base.webhook');
  assert.equal(hook.parameters.path,'restaurant-dev/inbox');
  assert.equal(hook.parameters.authentication,'headerAuth');
  assert.equal(hook.credentials.httpHeaderAuth.id,'restaurantHeaderAuth01');
});
test('all graph targets resolve to real supported nodes; no financial or external action exists',()=>{
  const types=new Set(['n8n-nodes-base.webhook','n8n-nodes-base.if','n8n-nodes-base.respondToWebhook','n8n-nodes-base.stickyNote']);
  const names=new Set(workflow.nodes.map(n=>n.name));
  for(const n of workflow.nodes) assert.ok(types.has(n.type));
  for(const [source,connection] of Object.entries(workflow.connections)) {
    assert.ok(names.has(source));
    for(const branch of connection.main) for(const target of branch) assert.ok(names.has(target.node));
  }
});
test('workflow export stores neither secrets nor execution payloads',()=>{
  const serialized=JSON.stringify(workflow);
  assert.ok(!serialized.includes('$env'));assert.ok(!serialized.includes('sk_test_'));
  assert.equal(workflow.settings.saveDataSuccessExecution,'none');assert.equal(workflow.settings.saveDataErrorExecution,'none');
  assert.equal(workflow.settings.executionTimeout,30);
});
test('fixtures identify a simulated customer and stable message identity',()=>{
  const valid=JSON.parse(readFileSync(new URL('./fixtures/valid-message.json',import.meta.url)));
  assert.match(valid.customerId,/^mock-/);assert.ok(valid.messageId&&valid.text);
});
