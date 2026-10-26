import { spawnSync } from 'node:child_process';
import { readFileSync } from 'node:fs';
export const root = new URL('../../', import.meta.url);
export const workflowPath = new URL('../workflows/01-development-inbox.json', import.meta.url);
export const workflow = JSON.parse(readFileSync(workflowPath, 'utf8'));
export function docker(args, {env = {}, input, quiet = true, includeStderr = false} = {}) {
  const result = spawnSync('docker', args, {cwd: root, env: {...process.env, ...env}, input,
    encoding: 'utf8', maxBuffer: 8 * 1024 * 1024, stdio: quiet ? ['pipe', 'pipe', 'pipe'] : 'inherit'});
  if (result.error) throw result.error;
  if (result.status !== 0) {
    // Never include input or environment values (credentials) in error output.
    throw new Error(`Docker ${args[0]} failed (exit ${result.status}). ${result.stderr?.trim() ?? ''}`);
  }
  return includeStderr ? `${result.stdout ?? ''}\n${result.stderr ?? ''}`.trim() : result.stdout?.trim() ?? '';
}
export function credential(token) {
  return [{id: 'restaurantHeaderAuth01',name: 'Restaurant development webhook',type: 'httpHeaderAuth',
    data: {name: 'X-Restaurant-Webhook-Token',value: token}}];
}
export async function ready(baseUrl, timeoutMs = 180000) {
  const end = Date.now() + timeoutMs;
  while (Date.now() < end) {
    try {
      const response = await fetch(`${baseUrl}/healthz/readiness`, {signal: AbortSignal.timeout(2000)});
      if (response.ok) return;
    } catch { /* startup and database migrations can briefly refuse connections */ }
    await new Promise(resolve => setTimeout(resolve, 1000));
  }
  throw new Error('n8n readiness did not become healthy within the startup timeout.');
}
export async function request(baseUrl, body, token, method = 'POST', timeoutMs = 15000) {
  const headers = {'Content-Type':'application/json'};
  if (token !== undefined) headers['X-Restaurant-Webhook-Token'] = token;
  const response = await fetch(`${baseUrl}/webhook/restaurant-dev/inbox`, {
    method, headers, ...(method === 'POST' ? {body:JSON.stringify(body)} : {}), signal:AbortSignal.timeout(timeoutMs)
  });
  const text = await response.text();
  let data; try { data = JSON.parse(text); } catch { data = text; }
  return {status:response.status,data,cacheControl:response.headers.get('cache-control')};
}

export function loadPrivateEnv(path) {
  const entries=readFileSync(path,'utf8').split(/\r?\n/).filter(line=>line.trim()&&!line.trim().startsWith('#'));
  return Object.fromEntries(entries.map(line=>{
    const split=line.indexOf('=');
    if(split<1) throw new Error('Invalid private environment file.');
    const name=line.slice(0,split).trim();let value=line.slice(split+1).trim();
    if((value.startsWith('"')&&value.endsWith('"'))||(value.startsWith("'")&&value.endsWith("'"))) value=value.slice(1,-1);
    return [name,value];
  }));
}
