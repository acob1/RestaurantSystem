import { ApplicationConfig, inject, provideAppInitializer } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { API_BASE } from './core/services/api';
import { routes } from './app.routes';
import { RuntimeConfig } from './core/services/runtime-config';
export const appConfig: ApplicationConfig = {
  providers: [provideHttpClient(), provideRouter(routes), RuntimeConfig,
    provideAppInitializer(() => inject(RuntimeConfig).load()),
    {provide: API_BASE, useFactory: () => inject(RuntimeConfig).apiBaseUrl}]
};
