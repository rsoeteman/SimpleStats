import { test as umbracoTest } from '@umbraco-cms/acceptance-test-helpers';

// Specs import `test` (and `expect`) from here instead of from the packages directly.
export { expect } from '@playwright/test';
export { ConstantHelper } from '@umbraco-cms/acceptance-test-helpers';

/**
 * Umbraco's own test fixture (gives you `umbracoUi` and `umbracoApi` next to `page`) with one
 * addition: after every test, the browser context's cookies are saved back to the shared storage
 * state (.tmp/state.json), which the next test starts from.
 *
 * Why: the backoffice keeps its OAuth tokens in cookies and rotates the refresh token whenever it
 * refreshes. A browser-only test would throw the rotated token away, so the next test starts from
 * an already-redeemed refresh token. Outside OpenIddict's 30-second leeway that counts as a token
 * replay, the whole session is revoked, and every later test lands on the login page.
 */
export const test = umbracoTest.extend<{ persistSession: void }>({
  persistSession: [
    async ({ page }, use) => {
      await use();

      const filePath = process.env.STORAGE_STATE_PATH;
      if (!filePath) return;
      try {
        // A logged-out context has nothing worth keeping - don't overwrite the shared state with it.
        const cookies = await page.context().cookies();
        if (!cookies.some((cookie) => cookie.name === '__Host-umbRefreshToken')) return;

        // Don't leak preferences (e.g. language) between tests.
        await page.evaluate(() => localStorage.clear()).catch(() => undefined);
        await page.context().storageState({ path: filePath });
      } catch {
        // The page may already be closed after a timeout.
      }
    },
    { auto: true },
  ],
});
