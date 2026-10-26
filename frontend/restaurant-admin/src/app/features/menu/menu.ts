import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';
import { Api, Category, MenuItem } from '../../core/services/api';
@Component({selector:'app-menu', imports:[CurrencyPipe, ReactiveFormsModule], template:`
<div class="page-heading"><div><div class="eyebrow">GOOD FOOD, WELL ORGANIZED</div><h1>Menu catalog<span>.</span></h1><p>Browse the menu served by your application database.</p></div><span class="pill">READ-ONLY · PHASE 01</span></div>
<section class="panel"><div class="catalog-toolbar"><label for="search">Find a dish<input id="search" type="search" placeholder="Search this page…" [formControl]="search"></label><label for="category">Category<select id="category" [formControl]="category"><option value="">All categories</option>@for (c of categories(); track c.id) {<option [value]="c.id">{{c.name}}</option>}</select></label><button class="button secondary" (click)="load()" [disabled]="loading()">Refresh</button></div>
@if (loading()) {<p role="status" class="empty">Loading menu…</p>} @else if (error()) {<div class="empty" role="alert"><h2>The menu could not be loaded</h2><p>Check the API and database connection, then try again.</p><button class="button primary" (click)="load()">Try again</button></div>} @else if (!filtered().length) {<div class="empty"><span>▤</span><h2>{{items().length ? 'No dishes match your search' : 'Your menu starts here'}}</h2><p>{{items().length ? 'Try another name or category.' : 'Run the development seed to add a sample Ghanaian menu. Menu editing arrives in Phase 5.'}}</p></div>} @else {<div class="table-scroll"><table><thead><tr><th>Dish</th><th>Category</th><th>Price</th><th>Availability</th></tr></thead><tbody>@for (item of filtered(); track item.id) {<tr><td><strong>{{item.name}}</strong><small class="dish-description">{{item.description}}</small></td><td>{{categoryName(item.categoryId)}}</td><td>{{item.priceMinor / 100 | currency:'GHS':'code'}}</td><td><span class="status-chip" [class.ok]="item.available">{{item.available ? 'Available' : 'Unavailable'}}</span></td></tr>}</tbody></table></div>}
<div class="pagination"><span>{{total()}} dishes in your catalog</span><div><button class="button secondary" [disabled]="page() === 1 || loading()" (click)="changePage(-1)">Previous</button><span>Page {{page()}}</span><button class="button secondary" [disabled]="page() * 20 >= total() || loading()" (click)="changePage(1)">Next</button></div></div></section>
<div class="notice"><span class="info-icon">i</span><p>All prices come from the backend in integer pesewas. Category management, images, variants, and availability editing are scheduled for Phase 5.</p></div>
`})
export class Menu {
  private readonly api = inject(Api); private readonly destroy = inject(DestroyRef);
  readonly items = signal<MenuItem[]>([]); readonly categories = signal<Category[]>([]);
  readonly loading = signal(true); readonly error = signal(false); readonly page = signal(1); readonly total = signal(0);
  readonly search = new FormControl('', {nonNullable:true}); readonly category = new FormControl('', {nonNullable:true});
  private readonly query = signal(''); private readonly selected = signal('');
  readonly filtered = computed(() => this.items().filter(i => i.name.toLowerCase().includes(this.query().toLowerCase()) && (!this.selected() || i.categoryId === this.selected())));
  constructor() { this.search.valueChanges.pipe(takeUntilDestroyed()).subscribe(q => this.query.set(q)); this.category.valueChanges.pipe(takeUntilDestroyed()).subscribe(c => this.selected.set(c)); this.load(); }
  load() { this.loading.set(true); this.error.set(false); forkJoin({menu:this.api.menu(this.page()), categories:this.api.categories()}).pipe(takeUntilDestroyed(this.destroy)).subscribe({next:r => {this.items.set(r.menu.items); this.total.set(r.menu.total); this.categories.set(r.categories); this.loading.set(false);},error:() => {this.error.set(true); this.loading.set(false);}}); }
  changePage(delta:number) {this.page.update(p => p + delta); this.load();}
  categoryName(id:string) {return this.categories().find(c => c.id === id)?.name ?? 'Uncategorized';}
}
