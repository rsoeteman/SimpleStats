import { defineConfig, devices } from '@playwright/test';
import * as path from 'node:path';

export const STORAGE_STATE = path.join(__dirname, '.tmp', 'state.json');

// tests/helpers/fixtures.ts saves the rotated session cookies here after every test.
process.env.STORAGE_STATE_PATH = STORAGE_STATE;

// Title of the HTML report. In CI, SIMPLESTATS_VERSION is the GitVersion package version, so
// published reports for different builds are easy to tell apart. Keep in sync with reportTitle()
// in scripts/generate-e2e-summary.mjs.
const packageVersion = process.env.SIMPLESTATS_VERSION;
const reportTitle = `SimpleStats ${packageVersion ? `${packageVersion} ` : ''}(Umbraco 18) - E2E report`;

export default defineConfig({
  testDir: './tests',
  timeout: 60 * 1000,
  expect: { timeout: 5000 },
  // All tests share one site and one database, so they run one at a time.
  fullyParallel: false,
  workers: 1,
  // 'html' is the technical report. e2e-summary-reporter.ts stages per-test results and
  // attachments so scripts/generate-e2e-summary.mjs can build the plain-language summary.html
  // after the run (see that script for why it can't write into playwright-report/ itself).
  reporter: [['html', { title: reportTitle, open: 'never' }], ['./reporters/e2e-summary-reporter.ts']],
  globalSetup: require.resolve('./global-setup'),
  globalTeardown: require.resolve('./global-teardown'),
  use: {
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    // The site runs on the untrusted ASP.NET Core dev certificate.
    ignoreHTTPSErrors: true,
    // Umbraco's backoffice marks elements with `data-mark`, not the Playwright-default
    // `data-testid` - getByTestId() silently matches nothing without this.
    testIdAttribute: 'data-mark',
  },
  projects: [
    {
      name: 'setup',
      testMatch: /auth\.setup\.ts/,
    },
    {
      name: 'chromium',
      testMatch: /\.spec\.ts$/,
      use: {
        ...devices['Desktop Chrome'],
        storageState: STORAGE_STATE,
      },
      dependencies: ['setup'],
    },
  ],
});
