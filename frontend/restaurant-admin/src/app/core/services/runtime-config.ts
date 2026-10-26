import { Injectable } from '@angular/core';
@Injectable()
export class RuntimeConfig {
  apiBaseUrl = '';
  async load() {
    const response = await fetch('/config.json', {cache: 'no-store'});
    if (!response.ok) throw new Error('Runtime API configuration could not be loaded.');
    const data: unknown = await response.json();
    if (!data || typeof data !== 'object' || !('apiBaseUrl' in data) || typeof data.apiBaseUrl !== 'string')
      throw new Error('Invalid runtime API configuration.');
    const base = data.apiBaseUrl.replace(/\/$/, '');
    if (base && !/^https:\/\//.test(base) && !/^http:\/\/(localhost|127\.0\.0\.1)(:\d+)?$/.test(base))
      throw new Error('API URL must use HTTPS except on localhost.');
    this.apiBaseUrl = base;
  }
}
