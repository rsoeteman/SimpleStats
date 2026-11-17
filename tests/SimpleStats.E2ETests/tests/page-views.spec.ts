import type { Page } from '@playwright/test';
import { test, expect } from './helpers/fixtures';

// The Starter Kit is imported unpublished, so these pages are published before they're visited.
// The site is fresh and this is the only spec that visits the front end, so the counts below are
// exact. A future spec that visits front-end pages must account for that (or use other pages).
const HOME = { name: 'Home', path: '/' };
const ABOUT = { name: 'About Us', path: '/about-us/' };

const DASHBOARD_PATH = '/umbraco/section/content/dashboard/simplestatsdashboard';

/** The "Times Accessed" cell of the dashboard row for the given node name. */
function timesAccessedCell(page: Page, nodeName: string) {
  const row = page
    .locator('simplestats-dashboard uui-table-row')
    .filter({ has: page.locator('uui-table-cell').first().getByText(nodeName, { exact: true }) });
  return row.locator('uui-table-cell').nth(1);
}

test('page views are counted on the dashboard', async ({ page, browser, umbracoApi, umbracoUi }, testInfo) => {
  // Includes the wait for the first commit of the buffered view counts.
  test.setTimeout(120_000);

  await test.step('Publish the Starter Kit site', async () => {
    // The whole tree, not just Home and About Us: the Starter Kit templates render navigation and
    // listings from the other pages and fail (500) while those are unpublished.
    const home = await umbracoApi.document.getByName(HOME.name);
    const documentApi = `${umbracoApi.baseUrl}/umbraco/management/api/v1/document/${home.id}`;
    const started = await umbracoApi.put(`${documentApi}/publish-with-descendants`, {
      includeUnpublishedDescendants: true,
      cultures: ['*'],
    });
    expect(started.ok(), `publish-with-descendants status ${started.status()}`).toBeTruthy();
    const { taskId } = await started.json();

    // Publishing a branch runs as a background task; poll its result until it's done.
    await expect(async () => {
      const result = await umbracoApi.get(`${documentApi}/publish-with-descendants/result/${taskId}`);
      expect(result.ok()).toBeTruthy();
      expect((await result.json()).isComplete).toBe(true);
    }).toPass({ timeout: 30_000, intervals: [1_000] });
  });

  await test.step('Visit Home once and About Us twice', async () => {
    // A separate context without the backoffice session, like a real visitor.
    // global-setup.ts puts the site URL in process.env.URL.
    const visitor = await browser.newContext({ ignoreHTTPSErrors: true, baseURL: process.env.URL });
    const visitorPage = await visitor.newPage();
    try {
      for (const node of [HOME, ABOUT, ABOUT]) {
        // Published content can take a moment to become routable, so retry until it serves.
        // A failed attempt returns 404 and resolves no content node, so it isn't counted.
        await expect(async () => {
          const response = await visitorPage.goto(node.path);
          expect(response?.status(), `${node.path} status`).toBe(200);
        }).toPass({ timeout: 30_000, intervals: [1_000] });
      }
      await testInfo.attach('About Us page as a visitor sees it', {
        body: await visitorPage.screenshot({ fullPage: false }),
        contentType: 'image/png',
      });
    } finally {
      await visitor.close();
    }
  });

  await test.step('Open the SimpleStats dashboard and check the counts', async () => {
    await umbracoUi.goToBackOffice();

    // Views are buffered in memory and committed by a background job (every 5 s on the test site,
    // first run 5 s after start-up), and the dashboard shows committed views only. So reload until
    // they show up instead of sleeping for a fixed time.
    await expect(async () => {
      await page.goto(new URL(DASHBOARD_PATH, process.env.URL).toString());
      await expect(timesAccessedCell(page, HOME.name)).toHaveText('1', { timeout: 5_000 });
      await expect(timesAccessedCell(page, ABOUT.name)).toHaveText('2', { timeout: 5_000 });
    }).toPass({ timeout: 60_000, intervals: [3_000] });

    await testInfo.attach('SimpleStats dashboard with the counted views', {
      body: await page.locator('simplestats-dashboard').screenshot(),
      contentType: 'image/png',
    });
  });
});
