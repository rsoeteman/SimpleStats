// Backoffice user that Umbraco's unattended install creates on the throwaway test site.
// Keep in sync with the UMBRACO__CMS__UNATTENDED__* values in Automation/e2e/New-SqliteTestSite.ps1.
// The site only ever runs on localhost for the duration of a test run, so these are not secrets.
export const E2E_USER_EMAIL = 'e2e@example.com';
export const E2E_USER_PASSWORD = 'E2eTest1234!';
