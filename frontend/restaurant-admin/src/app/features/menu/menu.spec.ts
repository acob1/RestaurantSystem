import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { Api } from '../../core/services/api';
import { Menu } from './menu';
describe('Menu', () => {
  const api = {menu:vi.fn(),categories:vi.fn()};
  beforeEach(() => {
    api.menu.mockReturnValue(of({items:[{id:'1',categoryId:'mains',name:'Jollof rice',description:'Freshly made',priceMinor:4550,available:true}],pageNumber:1,pageSize:20,total:1}));
    api.categories.mockReturnValue(of([{id:'mains',name:'Mains'}]));
    TestBed.configureTestingModule({imports:[Menu],providers:[{provide:Api,useValue:api}]});
  });
  it('renders integer minor prices and filters without altering server data', () => {
    const fixture=TestBed.createComponent(Menu);fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('45.50');
    fixture.componentInstance.search.setValue('missing');fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No dishes match your search');
    expect(fixture.componentInstance.items()).toHaveLength(1);
  });
  it('shows a recoverable error when the API fails', () => {
    api.menu.mockReturnValue(throwError(()=>new Error('offline')));
    const fixture=TestBed.createComponent(Menu);fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain('The menu could not be loaded');
  });
});
