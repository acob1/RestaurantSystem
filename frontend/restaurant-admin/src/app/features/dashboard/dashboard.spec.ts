import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { Api } from '../../core/services/api';
import { Dashboard } from './dashboard';
describe('Dashboard', () => {
  const api = {status: vi.fn(), restaurant: vi.fn(), ready: vi.fn()};
  beforeEach(() => {
    api.status.mockReturnValue(of({phase:'Phase 1',paymentMode:'Test',currency:'GHS',availableFeatures:[],plannedFeatures:[]}));
    api.restaurant.mockReturnValue(of({id:'1',name:'Accra Kitchen',currency:'GHS',timeZone:'Africa/Accra'}));
    api.ready.mockReturnValue(of('Healthy'));
    TestBed.configureTestingModule({imports:[Dashboard],providers:[provideRouter([]),{provide:Api,useValue:api}]});
  });
  it('renders server health and a real restaurant profile', async () => {
    const fixture = TestBed.createComponent(Dashboard); fixture.detectChanges();
    await new Promise(resolve => setTimeout(resolve, 20)); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Up and running');
    expect(fixture.nativeElement.textContent).toContain('Accra Kitchen');
    expect(fixture.nativeElement.textContent).toContain('No live payments');
    fixture.destroy();
  });
  it('reports failures without showing fabricated health', async () => {
    api.ready.mockReturnValue(throwError(() => new Error('offline')));
    const fixture = TestBed.createComponent(Dashboard); fixture.detectChanges();
    await new Promise(resolve => setTimeout(resolve, 20)); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain('Some services need attention');
    expect(fixture.componentInstance.databaseReady()).toBe(false); fixture.destroy();
  });
});
