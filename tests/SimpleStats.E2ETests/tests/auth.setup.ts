import { test as setup, expect } from '@playwright/test';
import { UiHelpers } from '@umbraco-cms/acceptance-test-helpers';
import { STORAGE_STATE } from '../playwright.config';

setup('authenticate', async ({ page }) => {
  setup.setTimeout(180_000);

  const umbracoUi = new UiHelpers(page);
  const usernameField = page.locator('[name="username"]');

  // Kestrel serves the /umbraco/login host page as soon as it accepts connections, well before
  // Umbraco's runtime has finished booting. Until then the login form doesn't render. There's no
  // clean server-side signal to poll for that, so retry the real navigation until the form shows.
  await expect(async () => {
    await umbracoUi.goToBackOffice();
    await expect(usernameField).toBeVisible({ timeout: 5_000 });
  }).toPass({ timeout: 150_000, intervals: [3_000] });

  await umbracoUi.login.enterEmail(process.env.UMBRACO_USER_LOGIN!);
  await umbracoUi.login.enterPassword(process.env.UMBRACO_USER_PASSWORD!);
  await umbracoUi.login.clickLoginButton();
  // Wait for the login form to be replaced by the backoffice shell.
  await expect(usernameField).toBeHidden({ timeout: 30_000 });

  await page.context().storageState({ path: STORAGE_STATE });
});
