import { execFileSync } from 'node:child_process';
import * as fs from 'node:fs';
import * as path from 'node:path';
import { E2E_USER_EMAIL, E2E_USER_PASSWORD } from './e2e-user';

const SITE_INFO_PATH = path.join(__dirname, '.tmp', 'site-info.json');
const NEW_SITE_SCRIPT = path.join(__dirname, '..', '..', 'Automation', 'e2e', 'New-SqliteTestSite.ps1');
const BOM_CHAR_CODE = 0xfeff;
// Ceiling, not a wait: setup moves on as soon as the site is ready. Normally this takes 1-2
// minutes; the ceiling stops a stalled `dotnet build` from burning the CI time budget.
const SETUP_BUDGET_MS = 10 * 60 * 1000;
// Lets New-SqliteTestSite.ps1 report its own timeout, with the log tail, before Node kills it.
const SCRIPT_GRACE_MS = 30 * 1000;
// Windows PowerShell locally; pwsh (PowerShell 7) elsewhere.
const POWERSHELL = process.platform === 'win32' ? 'powershell.exe' : 'pwsh';

interface SiteInfo {
  url: string;
  processId: number;
  sitePath: string;
  logPath: string;
}

export default async function globalSetup(): Promise<void> {
  fs.mkdirSync(path.dirname(SITE_INFO_PATH), { recursive: true });
  fs.rmSync(SITE_INFO_PATH, { force: true });

  console.log('Provisioning throwaway Umbraco SQLite site (this can take a few minutes)...');

  const args = [
    '-NoProfile',
    '-ExecutionPolicy', 'Bypass',
    '-File', NEW_SITE_SCRIPT,
    '-InfoOutputPath', SITE_INFO_PATH,
    '-TimeoutSeconds', String(SETUP_BUDGET_MS / 1000),
  ];
  if (process.env.E2E_SKIP_TEMPLATE_INSTALL === '1') {
    args.push('-SkipTemplateInstall');
  }
  if (process.env.E2E_SKIP_CLIENT_BUILD === '1') {
    args.push('-SkipClientBuild');
  }

  // stdio 'inherit' (not the default 'pipe') is required: New-SqliteTestSite.ps1 starts a
  // detached site process that keeps running, and on Windows that child inherits a handle to the
  // script's stdout. With 'pipe', execFileSync waits for an EOF that never comes and hangs
  // forever. The site info is read back from -InfoOutputPath instead of parsed from stdout.
  execFileSync(POWERSHELL, args, {
    stdio: 'inherit',
    timeout: SETUP_BUDGET_MS + SCRIPT_GRACE_MS,
  });

  if (!fs.existsSync(SITE_INFO_PATH)) {
    throw new Error(`New-SqliteTestSite.ps1 completed but did not write ${SITE_INFO_PATH}`);
  }

  // PowerShell's `Out-File -Encoding utf8` writes a UTF-8 BOM, which JSON.parse rejects.
  const raw = fs.readFileSync(SITE_INFO_PATH, 'utf-8');
  const withoutBom = raw.charCodeAt(0) === BOM_CHAR_CODE ? raw.slice(1) : raw;
  const siteInfo: SiteInfo = JSON.parse(withoutBom);

  process.env.URL = siteInfo.url;
  process.env.UMBRACO_USER_LOGIN = E2E_USER_EMAIL;
  process.env.UMBRACO_USER_PASSWORD = E2E_USER_PASSWORD;

  console.log(`Site ready at ${siteInfo.url} (pid ${siteInfo.processId}).`);
}
