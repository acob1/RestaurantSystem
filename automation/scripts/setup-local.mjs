import { randomBytes } from 'node:crypto';
import { readFileSync, writeFileSync, existsSync } from 'node:fs';
const path = new URL('../.env.local', import.meta.url);
if (existsSync(path)) {
  console.log('Existing automation/.env.local retained. Encryption key and webhook token were not changed.');
} else {
  const template = readFileSync(new URL('../local.env.example', import.meta.url), 'utf8');
  const content = template.replace('REPLACE_WITH_STABLE_RANDOM_32_BYTE_SECRET', randomBytes(32).toString('hex'))
    .replace('REPLACE_WITH_RANDOM_WEBHOOK_SECRET', randomBytes(32).toString('hex'));
  writeFileSync(path, content, {mode: 0o600, flag: 'wx'});
  console.log('Created ignored automation/.env.local with a stable random encryption key and webhook token.');
}
console.log('Start with the local-db profile described in automation/README.md.');
