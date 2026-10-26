import { Injectable, InjectionToken, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
export const API_BASE = new InjectionToken<string>('API_BASE');
export interface SystemStatus { phase: string; paymentMode: string; currency: string; availableFeatures: string[]; plannedFeatures: string[]; }
export interface RestaurantProfile { id: string; name: string; currency: string; timeZone: string; }
export interface MenuItem { id: string; categoryId: string; name: string; description: string; priceMinor: number; available: boolean; }
export interface Category { id: string; name: string; }
export interface Page<T> { items: T[]; pageNumber: number; pageSize: number; total: number; }
@Injectable({providedIn: 'root'})
export class Api {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_BASE);
  status() { return this.http.get<SystemStatus>(`${this.base}/api/v1/system/status`); }
  restaurant() { return this.http.get<RestaurantProfile>(`${this.base}/api/v1/restaurant`); }
  ready() { return this.http.get(`${this.base}/health/ready`, {responseType: 'text'}); }
  categories() { return this.http.get<Category[]>(`${this.base}/api/v1/menu/categories`); }
  menu(page = 1) { return this.http.get<Page<MenuItem>>(`${this.base}/api/v1/menu/items`, {params: {page, pageSize: 20}}); }
}
