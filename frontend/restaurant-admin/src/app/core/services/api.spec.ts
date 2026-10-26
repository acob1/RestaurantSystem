import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { Api, API_BASE } from './api';
describe('Api', () => {
  beforeEach(() => TestBed.configureTestingModule({providers:[provideHttpClient(),provideHttpClientTesting(),{provide:API_BASE,useValue:'https://api.example.com'}]}));
  afterEach(() => TestBed.inject(HttpTestingController).verify());
  it('requests paginated menu data from the configured ASP.NET API', () => {
    TestBed.inject(Api).menu(2).subscribe(result => expect(result.total).toBe(0));
    const request = TestBed.inject(HttpTestingController).expectOne('https://api.example.com/api/v1/menu/items?page=2&pageSize=20');
    expect(request.request.method).toBe('GET'); request.flush({items:[],pageNumber:2,pageSize:20,total:0});
  });
});
