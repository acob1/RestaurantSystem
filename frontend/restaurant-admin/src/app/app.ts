import { Component, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
@Component({selector: 'app-root', imports: [RouterLink, RouterLinkActive, RouterOutlet], template: `
<div class="workspace">
  <aside class="sidebar" [class.mobile-open]="navOpen()">
    <a class="brand" routerLink="/" (click)="navOpen.set(false)"><span class="brand-mark">ak<span>•</span></span><span>accra kitchen<small>RESTAURANT WORKSPACE</small></span></a>
    <div class="workspace-label">YOUR RESTAURANT</div>
    <nav aria-label="Main navigation">
      <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{exact:true}" (click)="navOpen.set(false)"><span aria-hidden="true">◫</span> Overview</a>
      <a routerLink="/menu" routerLinkActive="active" (click)="navOpen.set(false)"><span aria-hidden="true">▤</span> Menu catalog</a>
      <a routerLink="/setup" routerLinkActive="active" (click)="navOpen.set(false)"><span aria-hidden="true">⚙</span> System setup</a>
    </nav>
    <div class="workspace-label coming-label">COMING NEXT</div>
    <div class="planned-nav"><span>Orders</span><small>Phase 5</small></div>
    <div class="planned-nav"><span>Payment review</span><small>Phase 6</small></div>
    <div class="planned-nav"><span>Kitchen</span><small>Phase 7</small></div>
    <div class="sidebar-bottom"><span class="small-dot"></span> Development workspace<p>Ghana · GHS · Africa/Accra</p><span class="phase-tag">PHASE 01 / FOUNDATION</span></div>
  </aside>
  <div class="main-shell">
    <header class="topbar"><div class="flex items-center gap-3"><button class="mobile-toggle" aria-label="Toggle navigation" [attr.aria-expanded]="navOpen()" (click)="navOpen.set(!navOpen())">☰</button><span class="breadcrumb">Workspace <span>/</span> Administration</span></div><span class="test-badge"><span class="small-dot"></span> Test environment</span></header>
    <main id="main-content"><router-outlet /></main>
    <footer>Accra Kitchen <span>Built for good food. Ready for what’s next.</span><span>GHS · Ghana</span></footer>
  </div>
</div>`})
export class App { readonly navOpen = signal(false); }
