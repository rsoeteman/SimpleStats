# SimpleStats E2E tests

Playwright end-to-end tests that run against a real, freshly installed Umbraco 18 site with SimpleStats installed.

Each run:

1. `global-setup.ts` runs `Automation/e2e/New-SqliteTestSite.ps1`. That script builds the SimpleStats.Client backoffice assets, creates a throwaway Umbraco 18 site on SQLite with The Starter Kit under `SimpleStatsE2E` in the temp folder (`%TEMP%` on Windows, `$TMPDIR` or `/tmp` on Linux), references `src/SimpleStats`, builds the site and starts it on `https://localhost:44351`. It then reads Umbraco's log and sends no request until Umbraco's first-boot unattended upgrade has finished. If that upgrade stalls (it can on SQLite), the script exits with code 3 and `global-setup.ts` retries on a brand-new site, up to 3 attempts.
2. `tests/auth.setup.ts` waits until Umbraco has finished booting, signs in as the unattended-install user and saves the session to `.tmp/state.json`.
3. The specs in `tests/*.spec.ts` run one at a time against that site, already signed in.
4. `global-teardown.ts` runs `Automation/e2e/Stop-SqliteTestSite.ps1`, which stops the site and removes its folder.

## Prerequisites

- .NET 10 SDK
- Node.js 20+
- A local ASP.NET Core dev certificate: `dotnet dev-certs https` (no need to trust it)

## Running

```powershell
cd tests/SimpleStats.E2ETests
npm install
npm run install:browsers   # once
npm test                   # provisions the site, runs the suite, tears the site down
npm run summary            # writes playwright-report/summary.html and SUMMARY.md
npm run report             # opens the Playwright HTML report
```

A full run takes about 2 minutes, most of which is Umbraco's first boot.

## Environment switches

| Variable | Effect |
|---|---|
| `E2E_KEEP_SITE=1` | Stops the site after the run but keeps its folder (database, logs) for debugging |
| `E2E_SKIP_TEMPLATE_INSTALL=1` | Reuses the already-installed Umbraco templates in `%TEMP%\SimpleStatsE2E\template-hive` |
| `E2E_SKIP_CLIENT_BUILD=1` | Skips `npm run build` of SimpleStats.Client (CI builds it in an earlier step) |
| `SIMPLESTATS_VERSION` | Package version shown in the report titles (set by CI from GitVersion) |

The test site commits buffered page views every 5 seconds (`SimpleStats__CommitIntervalSeconds=5`) instead of the default 60. It also makes the first commit 5 seconds after start-up (`SimpleStats__CommitDelaySeconds=5`) instead of Umbraco's default 3 minutes.

## Writing specs

- Import `test` and `expect` from `./helpers/fixtures`. The fixture gives you `umbracoUi` and `umbracoApi` next to `page`, and keeps the backoffice session alive between tests.
- Start with `await umbracoUi.goToBackOffice()`. A new test page only has the session cookies, not a loaded backoffice.
- Umbraco marks elements with `data-mark`, and `getByTestId()` is configured to use it.
- Attach evidence with `testInfo.attach(name, { body, contentType })`, not `page.screenshot({ path })`. Only attachments end up in the published report and in `summary.html`.
- Give each new test a plain-language title and description in `TITLE_MAP` in `scripts/generate-e2e-summary.mjs`.

## Reports

- `playwright-report/index.html`: the full Playwright report with steps, traces and screenshots.
- `playwright-report/summary.html`: one card per test with a plain-language title, pass/fail and its screenshots, for readers who don't know Playwright.

In CI, both are published to the repository's GitHub Pages site, and the workflow run's summary links to them.

## Cleaning up after an aborted run

If a run is killed halfway, a `dotnet` site process can stay running on port 44351. Stop it by its PID (`Get-NetTCPConnection -LocalPort 44351`, then `Stop-Process -Id <pid>`). Old site folders under `%TEMP%\SimpleStatsE2E` can be removed by hand.
