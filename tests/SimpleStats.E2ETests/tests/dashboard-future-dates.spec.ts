import { test, expect } from './helpers/fixtures';

const DASHBOARD_PATH = '/umbraco/section/content/dashboard/simplestatsdashboard';

/** yyyy-MM-dd, the value format of a date input. */
function isoDate(date: Date): string {
  return date.toISOString().slice(0, 10);
}

test('future date range shows no records', async ({ page, umbracoUi }, testInfo) => {
  // A range that starts tomorrow can't hold any recorded visits, whatever other specs did.
  const start = new Date();
  start.setUTCDate(start.getUTCDate() + 1);
  const end = new Date(start);
  end.setUTCDate(end.getUTCDate() + 30);
  const startValue = isoDate(start);
  const endValue = isoDate(end);

  await umbracoUi.goToBackOffice();
  await page.goto(new URL(DASHBOARD_PATH, process.env.URL).toString());

  const dashboard = page.locator('simplestats-dashboard');

  await test.step('Pick a date range in the future and apply it', async () => {
    // uui-input wraps a native <input type="date">; filling it fires the change event the
    // dashboard listens for.
    await dashboard.locator('#start-date input').fill(startValue);
    await dashboard.locator('#end-date input').fill(endValue);
    await dashboard.getByRole('button', { name: 'Apply' }).click();
  });

  await test.step('The dashboard shows the no-records message', async () => {
    await expect(dashboard.getByText('No records found', { exact: true })).toBeVisible();
    await expect(
      dashboard.getByText(`No content nodes were accessed between ${startValue} and ${endValue}.`, { exact: false }),
    ).toBeVisible();
    await expect(dashboard.locator('uui-table')).toHaveCount(0);

    await testInfo.attach('SimpleStats dashboard for a future date range', {
      body: await dashboard.screenshot(),
      contentType: 'image/png',
    });
  });
});
