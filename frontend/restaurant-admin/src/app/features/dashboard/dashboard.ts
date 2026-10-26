import { Component, DestroyRef, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, forkJoin, of, switchMap, timer } from 'rxjs';
import { Api, RestaurantProfile, SystemStatus } from '../../core/services/api';
@Component({selector: 'app-dashboard', imports: [RouterLink, DatePipe], template: `
<div class="page-heading"><div><div class="eyebrow">YOUR RESTAURANT, CONNECTED</div><h1>Good things start here<span>.</span></h1><p>Your workspace is taking shape. Keep an eye on the essentials.</p></div><button class="button secondary" (click)="refresh()" [disabled]="loading()">↻ <span>Refresh status</span></button></div>
<section class="welcome-panel"><div><span class="pill light">PHASE 01</span><h2>A strong foundation.<br>A smoother service.</h2><p>Your API, database, and menu in one place.<br>Connect the essentials before taking your first order.</p><a class="button cream" routerLink="/setup">View setup guide <span aria-hidden="true">↗</span></a></div><div class="foundation-art" aria-hidden="true"><div class="orbit orbit-one"></div><div class="orbit orbit-two"></div><div class="plate"><div class="plate-inner">ak<span>•</span></div></div><span class="art-label label-one">Fresh connections</span><span class="art-label label-two">Made in Accra</span><span class="art-star">✳</span></div></section>
<div class="section-heading"><h2>Workspace health</h2><span class="muted">@if (checkedAt()) { Updated {{ checkedAt() | date:'HH:mm:ss':'Africa/Accra' }} GMT } @else { Checking services… }</span></div>
<div class="health-grid" aria-live="polite">
  <article class="health-card"><div class="card-top"><span class="card-icon">↗</span><span class="status-chip" [class.ok]="status()" [class.bad]="!loading() && !status()">{{ loading() ? 'Checking' : status() ? 'Connected' : 'Unavailable' }}</span></div><p>Application API</p><h3>{{ status() ? 'Up and running' : loading() ? 'Connecting…' : 'Needs attention' }}</h3><small>ASP.NET Core 10 · REST API</small></article>
  <article class="health-card"><div class="card-top"><span class="card-icon">▱</span><span class="status-chip" [class.ok]="databaseReady()" [class.bad]="!loading() && !databaseReady()">{{ loading() ? 'Checking' : databaseReady() ? 'Connected' : 'Unavailable' }}</span></div><p>Business database</p><h3>{{ databaseReady() ? 'Ready for service' : loading() ? 'Connecting…' : 'Setup required' }}</h3><small>PostgreSQL · Migration aware</small></article>
  <article class="health-card"><div class="card-top"><span class="card-icon">₵</span><span class="status-chip">Test only</span></div><p>Payment environment</p><h3>No live payments</h3><small>Ghana cedi · GHS</small></article>
</div>
@if (!loading() && (!status() || !databaseReady())) { <div role="alert" class="notice warning"><strong>Some services need attention.</strong> Confirm the API is running and apply database migrations. Free hosting may take time to wake up. <a routerLink="/setup">Open setup guide →</a></div> }
<div class="lower-grid"><section class="panel"><div class="panel-title"><h2>Restaurant details</h2><span class="subtle-icon">⌂</span></div>@if (restaurant(); as r) {<div class="restaurant-identity"><span class="restaurant-avatar">ak.</span><div><h3>{{r.name}}</h3><p>Your restaurant profile</p></div></div><dl class="detail-list"><div><dt>Market</dt><dd>Ghana</dd></div><div><dt>Currency</dt><dd>{{r.currency}} · Ghana cedi</dd></div><div><dt>Timezone</dt><dd>{{r.timeZone}}</dd></div></dl>} @else {<div class="empty compact"><span>⌂</span><h3>{{loading() ? 'Loading your restaurant…' : 'Your profile is not ready yet'}}</h3><p>Apply migrations and run the development seed to create your restaurant profile.</p></div>}<a class="text-link" routerLink="/menu">Explore your menu <span>→</span></a></section>
<section class="panel"><div class="panel-title"><h2>The road to your first order</h2><span class="pill">BUILD ROADMAP</span></div><ol class="roadmap"><li class="current"><span class="step">01</span><div><h3>Connect the foundation <span>In progress</span></h3><p>API, database, dashboard, and deployment setup.</p></div></li><li><span class="step">02</span><div><h3>Bring conversations to life</h3><p>Staff access, WhatsApp, and AI orchestration.</p></div></li><li><span class="step">03</span><div><h3>From menu to mealtime</h3><p>Carts, verified payments, and kitchen operations.</p></div></li></ol></section></div>
<div class="notice"><span class="info-icon">i</span><p><strong>A workspace for development.</strong> Orders and payment processing will be enabled in later phases. Free-tier hosting is for testing and requires an always-on upgrade before launch.</p></div>
`})
export class Dashboard {
  private readonly api = inject(Api);
  private readonly destroyRef = inject(DestroyRef);
  readonly status = signal<SystemStatus | null>(null);
  readonly restaurant = signal<RestaurantProfile | null>(null);
  readonly databaseReady = signal(false);
  readonly loading = signal(true);
  readonly checkedAt = signal<Date | null>(null);
  constructor() { timer(0, 30000).pipe(switchMap(() => this.load()), takeUntilDestroyed(this.destroyRef)).subscribe(result => this.apply(result)); }
  private load() { return forkJoin({status: this.api.status().pipe(catchError(() => of(null))), restaurant: this.api.restaurant().pipe(catchError(() => of(null))), ready: this.api.ready().pipe(catchError(() => of(null)))}); }
  private apply(result: {status: SystemStatus | null; restaurant: RestaurantProfile | null; ready: string | null}) {
    this.status.set(result.status); this.restaurant.set(result.restaurant); this.databaseReady.set(result.ready === 'Healthy'); this.loading.set(false); this.checkedAt.set(new Date());
  }
  refresh() { this.loading.set(true); this.load().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(result => this.apply(result)); }
}
