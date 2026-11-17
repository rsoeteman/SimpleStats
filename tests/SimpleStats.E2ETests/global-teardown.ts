import { execFileSync } from 'node:child_process';
import * as fs from 'node:fs';
import * as os from 'node:os';
import * as path from 'node:path';

const SITE_INFO_PATH = path.join(__dirname, '.tmp', 'site-info.json');
const STOP_SITE_SCRIPT = path.join(__dirname, '..', '..', 'Automation', 'e2e', 'Stop-SqliteTestSite.ps1');
const BOM_CHAR_CODE = 0xfeff;
const POWERSHELL = process.platform === 'win32' ? 'powershell.exe' : 'pwsh';

export default async function globalTeardown(): Promise<void> {
  if (!fs.existsSync(SITE_INFO_PATH)) {
    console.warn('No site-info.json found; skipping teardown (setup may have failed before provisioning).');
    return;
  }

  try {
    const raw = fs.readFileSync(SITE_INFO_PATH, 'utf-8');
    const withoutBom = raw.charCodeAt(0) === BOM_CHAR_CODE ? raw.slice(1) : raw;
    const siteInfo = JSON.parse(withoutBom);

    const args = [
      '-NoProfile',
      '-ExecutionPolicy', 'Bypass',
      '-File', STOP_SITE_SCRIPT,
      '-ProcessId', String(siteInfo.processId),
      '-SitePath', siteInfo.sitePath,
    ];
    // E2E_KEEP_SITE=1 stops the site but keeps its folder (database, logs) for debugging.
    if (process.env.E2E_KEEP_SITE === '1') {
      args.push('-KeepFiles');
    }

    execFileSync(POWERSHELL, args, { stdio: 'inherit' });
  } catch (err) {
    console.warn(`Teardown of the e2e test site hit a problem (the site may need manual cleanup under ${path.join(os.tmpdir(), 'SimpleStatsE2E')}):`, err);
  } finally {
    fs.rmSync(SITE_INFO_PATH, { force: true });
  }
}
