import { Routes } from '@angular/router';
export const routes: Routes = [
  {path: '', pathMatch: 'full', loadComponent: () => import('./features/dashboard/dashboard').then(m => m.Dashboard)},
  {path: 'menu', loadComponent: () => import('./features/menu/menu').then(m => m.Menu)},
  {path: 'setup', loadComponent: () => import('./features/settings/setup').then(m => m.Setup)},
  {path: '**', redirectTo: ''}
];
