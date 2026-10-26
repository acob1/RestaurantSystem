import { test, expect } from '@playwright/test';
test('workspace reads the API and opens the menu', async ({page, isMobile}) => {
  // This is a real API smoke test. Seed the controlled PostgreSQL database first.
  await page.goto('/');
  await expect(page.getByRole('heading',{name:'Good things start here.'})).toBeVisible();
  await expect(page.getByRole('heading',{name:'Up and running'})).toBeVisible();
  await expect(page.getByRole('heading',{name:'Ready for service'})).toBeVisible();
  if (isMobile) await page.getByRole('button',{name:'Toggle navigation'}).click();
  await page.getByRole('link',{name:'Menu catalog'}).click();
  await expect(page.getByRole('heading',{name:'Menu catalog.'})).toBeVisible();
  await expect(page.getByRole('cell',{name:'Jollof rice with chicken',exact:false})).toBeVisible();
  await page.getByRole('searchbox',{name:'Find a dish'}).fill('does not exist');
  await expect(page.getByRole('heading',{name:'No dishes match your search'})).toBeVisible();
});
