import { writeFileSync } from 'node:fs';
const apiBaseUrl = (process.env.PUBLIC_API_BASE_URL ?? '').replace(/\/$/, '');
if (process.env.CI && !apiBaseUrl && process.env.RENDER) throw new Error('PUBLIC_API_BASE_URL is required on Render.');
if (apiBaseUrl && !/^https:\/\//.test(apiBaseUrl)) throw new Error('Hosted API must use HTTPS.');
writeFileSync(new URL('../public/config.json', import.meta.url), JSON.stringify({apiBaseUrl}, null, 2) + '\n');
