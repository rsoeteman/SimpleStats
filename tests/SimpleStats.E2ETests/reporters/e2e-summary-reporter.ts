import * as fs from 'node:fs';
import * as path from 'node:path';
import type { Reporter, TestCase, TestResult } from '@playwright/test/reporter';

// Playwright's built-in 'html' reporter deletes and rebuilds playwright-report/ from scratch in
// its own onEnd(), which runs after every reporter's onEnd() but at an unspecified relative order
// - so nothing written here can safely land inside playwright-report/ during the run. Instead,
// this reporter stages results/attachments under test-results/ (Playwright's outputDir, only
// cleared once at the very start of the whole run), and a separate script
// (scripts/generate-e2e-summary.mjs), run as its own CI step *after* `npm test` has fully exited,
// copies what it needs into the now-final playwright-report/.
const SUMMARY_DIR = path.join(__dirname, '..', 'test-results', 'e2e-summary');
const ASSETS_DIR = path.join(SUMMARY_DIR, 'assets');

interface SummaryAttachment {
  name: string;
  contentType: string;
  assetPath: string;
}

interface SummaryTestResult {
  title: string;
  file: string;
  status: TestResult['status'];
  attachments: SummaryAttachment[];
}

function slugify(value: string): string {
  return value
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/(^-|-$)/g, '');
}

export default class E2ESummaryReporter implements Reporter {
  private results: SummaryTestResult[] = [];

  onTestEnd(test: TestCase, result: TestResult): void {
    const specFile = path.basename(test.location.file);
    const slug = `${slugify(specFile.replace(/\.spec\.ts$/, ''))}--${slugify(test.title)}`;
    const testAssetsDir = path.join(ASSETS_DIR, slug);

    const attachments: SummaryAttachment[] = [];
    for (const attachment of result.attachments) {
      if (!attachment.body && !attachment.path) continue;

      fs.mkdirSync(testAssetsDir, { recursive: true });
      const ext = attachment.contentType.includes('png')
        ? '.png'
        : path.extname(attachment.path ?? attachment.name) || '';
      const fileName = attachment.name.includes('.') ? attachment.name : `${attachment.name}${ext}`;
      const destPath = path.join(testAssetsDir, fileName);

      if (attachment.body) {
        fs.writeFileSync(destPath, attachment.body);
      } else if (attachment.path) {
        fs.copyFileSync(attachment.path, destPath);
      }

      attachments.push({
        name: attachment.name,
        contentType: attachment.contentType,
        // Stored relative to ASSETS_DIR so the generator script can re-root it under
        // playwright-report/summary-assets/ without caring about absolute paths.
        assetPath: path.join(slug, fileName).replace(/\\/g, '/'),
      });
    }

    this.results.push({
      title: test.title,
      file: specFile,
      status: result.status,
      attachments,
    });
  }

  onEnd(): void {
    fs.mkdirSync(SUMMARY_DIR, { recursive: true });
    fs.writeFileSync(
      path.join(SUMMARY_DIR, 'results.json'),
      JSON.stringify({ generatedAt: new Date().toISOString(), tests: this.results }, null, 2)
    );
  }
}
