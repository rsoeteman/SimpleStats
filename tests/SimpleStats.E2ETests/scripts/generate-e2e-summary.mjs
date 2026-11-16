// Generates a plain-language companion to the Playwright HTML report: SUMMARY.md and
// summary.html, listing each test with a logical title/description, its pass/fail status, and
// its screenshots/exported files from this run. Runs as its own CI step *after* `npm test` has
// fully exited (see reporters/e2e-summary-reporter.ts for why), so it can safely write into
// playwright-report/ without the built-in 'html' reporter's own onEnd() wiping it out.
//
// Must never fail the pipeline: any missing/malformed input falls back to a minimal placeholder
// page rather than throwing.
import * as fs from 'node:fs';
import * as path from 'node:path';
import * as url from 'node:url';

const ROOT = path.dirname(url.fileURLToPath(import.meta.url));
const E2E_TESTS_DIR = path.join(ROOT, '..');
const RESULTS_JSON = path.join(E2E_TESTS_DIR, 'test-results', 'e2e-summary', 'results.json');
const ASSETS_SRC_DIR = path.join(E2E_TESTS_DIR, 'test-results', 'e2e-summary', 'assets');
const REPORT_DIR = path.join(E2E_TESTS_DIR, 'playwright-report');
const ASSETS_DEST_DIR = path.join(REPORT_DIR, 'summary-assets');

// Labels every page with (in CI) the GitVersion package version, so published reports for
// different builds can't be mistaken for each other. Keep in sync with reportTitle in
// playwright.config.ts.
function reportTitle() {
  const packageVersion = process.env.SIMPLESTATS_VERSION;
  return `SimpleStats ${packageVersion ? `${packageVersion} ` : ''}(Umbraco 18) - E2E test summary`;
}
const SUMMARY_TITLE = reportTitle();

// Maps a test's technical title to a plain-language title and one-line description for readers
// who don't know Playwright. Tests without an entry fall back to their own title.
const TITLE_MAP = {
  authenticate: {
    title: 'Sign in to the backoffice',
    description:
      'Waits for the freshly installed Umbraco site to finish booting, then signs in as the test editor. Every other check starts from this session.',
  },
};

function describe(rawTitle) {
  return TITLE_MAP[rawTitle] ?? { title: rawTitle, description: '' };
}

function statusBadge(status) {
  return status === 'passed' ? { emoji: '✅', label: 'Passed' } : { emoji: '❌', label: status };
}

function isImage(contentType) {
  return contentType.startsWith('image/');
}

// Screenshot attachment names are already descriptive step titles set by the tests themselves
// (e.g. "01-workspace-loaded", "04-export-downloaded") - strip the leading step number and turn
// the rest into a readable caption instead of maintaining a separate, easily-stale lookup table.
function captionFor(attachmentName) {
  return attachmentName
    .replace(/^\d+[-_]*/, '')
    .replace(/[-_]+/g, ' ')
    .trim()
    .replace(/\b\w/g, (c) => c.toUpperCase());
}

// Screenshots are the visual proof and read best one per row; downloadable files (exports, logs) are
// reference material, so they're grouped and rendered after every screenshot in the test.
function splitAttachments(attachments) {
  const images = attachments.filter((a) => isImage(a.contentType));
  const files = attachments.filter((a) => !isImage(a.contentType));
  return { images, files };
}

function loadResults() {
  const raw = fs.readFileSync(RESULTS_JSON, 'utf-8');
  const parsed = JSON.parse(raw);
  if (!Array.isArray(parsed.tests)) throw new Error('results.json missing tests array');
  return parsed;
}

function copyAssets() {
  fs.rmSync(ASSETS_DEST_DIR, { recursive: true, force: true });
  if (fs.existsSync(ASSETS_SRC_DIR)) {
    fs.cpSync(ASSETS_SRC_DIR, ASSETS_DEST_DIR, { recursive: true });
  }
}

function renderMarkdown(tests) {
  const lines = [];
  lines.push(`# ${SUMMARY_TITLE}`);
  lines.push('');
  lines.push(
    'This page explains, in plain language, what the automated end-to-end suite checks in a real ' +
      'running Umbraco site, whether each check passed on this run, and the actual screenshots / ' +
      'exported files it produced. For the full technical report (steps, traces, timings), see ' +
      '[the Playwright report](index.html).'
  );
  lines.push('');

  for (const test of tests) {
    const { title, description } = describe(test.title);
    const badge = statusBadge(test.status);
    lines.push(`## ${badge.emoji} ${title}`);
    lines.push('');
    if (description) {
      lines.push(description);
      lines.push('');
    }
    lines.push(`**Status:** ${badge.label}`);
    lines.push('');

    const { images, files } = splitAttachments(test.attachments);

    for (const attachment of images) {
      const assetPath = `summary-assets/${attachment.assetPath}`;
      lines.push(`![${attachment.name}](${assetPath})`);
      lines.push(`*${captionFor(attachment.name)}*`);
      lines.push('');
    }

    if (files.length > 0) {
      lines.push('**Files:**');
      lines.push('');
      for (const attachment of files) {
        const assetPath = `summary-assets/${attachment.assetPath}`;
        lines.push(`- [Download ${attachment.name}](${assetPath})`);
      }
      lines.push('');
    }
  }

  return lines.join('\n');
}

function escapeHtml(value) {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

function renderHtml(tests) {
  const sections = tests
    .map((test) => {
      const { title, description } = describe(test.title);
      const badge = statusBadge(test.status);
      const badgeClass = test.status === 'passed' ? 'pass' : 'fail';

      const { images, files } = splitAttachments(test.attachments);

      const screenshots = images
        .map((attachment) => {
          const assetPath = `summary-assets/${attachment.assetPath}`;
          return `
        <figure class="screenshot">
          <a href="${assetPath}" target="_blank"><img src="${assetPath}" alt="${escapeHtml(attachment.name)}" loading="lazy"></a>
          <figcaption>${escapeHtml(captionFor(attachment.name))}</figcaption>
        </figure>`;
        })
        .join('\n');

      const fileLinks = files
        .map((attachment) => {
          const assetPath = `summary-assets/${attachment.assetPath}`;
          return `<a class="file" href="${assetPath}" download>⬇ ${escapeHtml(attachment.name)}</a>`;
        })
        .join('\n');

      return `
      <section class="test">
        <h2><span class="badge ${badgeClass}">${badge.emoji} ${badge.label}</span> ${escapeHtml(title)}</h2>
        ${description ? `<p class="description">${escapeHtml(description)}</p>` : ''}
        <div class="screenshots">${screenshots}</div>
        ${files.length > 0 ? `<div class="files">${fileLinks}</div>` : ''}
      </section>`;
    })
    .join('\n');

  return `<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>${escapeHtml(SUMMARY_TITLE)}</title>
<style>
  :root { color-scheme: light dark; }
  body {
    font-family: -apple-system, Segoe UI, Roboto, Helvetica, Arial, sans-serif;
    max-width: 900px;
    margin: 2rem auto;
    padding: 0 1.5rem;
    line-height: 1.5;
    color: #1a1a1a;
    background: #fff;
  }
  h1 { margin-bottom: 0.25rem; }
  .intro { color: #444; margin-bottom: 2rem; }
  .intro a { color: #0969da; }
  section.test {
    border: 1px solid #d0d7de;
    border-radius: 8px;
    padding: 1.25rem 1.5rem;
    margin-bottom: 1.25rem;
  }
  h2 { margin-top: 0; font-size: 1.15rem; }
  .badge {
    display: inline-block;
    font-size: 0.85rem;
    font-weight: 600;
    padding: 0.15rem 0.6rem;
    border-radius: 999px;
    margin-right: 0.5rem;
  }
  .badge.pass { background: #dafbe1; color: #116329; }
  .badge.fail { background: #ffebe9; color: #82071e; }
  .description { color: #444; }
  .screenshots { display: flex; flex-direction: column; gap: 1.5rem; margin-top: 1rem; }
  figure.screenshot { margin: 0; }
  figure.screenshot img {
    display: block;
    width: 100%;
    height: auto;
    border: 1px solid #d0d7de;
    border-radius: 6px;
  }
  figure.screenshot figcaption {
    margin-top: 0.4rem;
    font-size: 0.9rem;
    color: #57606a;
  }
  .files { display: flex; flex-wrap: wrap; gap: 0.75rem; margin-top: 1.25rem; }
  .files a.file {
    display: inline-block;
    padding: 0.5rem 0.85rem;
    border: 1px solid #d0d7de;
    border-radius: 6px;
    text-decoration: none;
    color: #0969da;
    font-size: 0.9rem;
  }
  @media (prefers-color-scheme: dark) {
    body { color: #e6edf3; background: #0d1117; }
    .intro { color: #9198a1; }
    section.test { border-color: #30363d; }
    .description { color: #9198a1; }
    figure.screenshot img { border-color: #30363d; }
    figure.screenshot figcaption { color: #9198a1; }
    .files a.file { border-color: #30363d; }
  }
</style>
</head>
<body>
  <h1>${escapeHtml(SUMMARY_TITLE)}</h1>
  <p class="intro">
    Plain-language results from the automated end-to-end suite, run against a real Umbraco site.
    For the full technical report (steps, traces, timings), see
    <a href="index.html">the Playwright report</a>.
  </p>
  ${sections}
</body>
</html>
`;
}

function writeFallback(reason) {
  fs.mkdirSync(REPORT_DIR, { recursive: true });
  const message =
    `# ${SUMMARY_TITLE}\n\n` +
    `No structured results were available for this run (${reason}). ` +
    `See [the Playwright report](index.html) for full details.\n`;
  fs.writeFileSync(path.join(REPORT_DIR, 'SUMMARY.md'), message);
  fs.writeFileSync(
    path.join(REPORT_DIR, 'summary.html'),
    `<!doctype html><html><head><meta charset="utf-8"><title>${escapeHtml(SUMMARY_TITLE)}</title></head>` +
      `<body><h1>${escapeHtml(SUMMARY_TITLE)}</h1><p>No structured results were available for this run (${escapeHtml(
        reason
      )}). See <a href="index.html">the Playwright report</a> for full details.</p></body></html>`
  );
}

function main() {
  let data;
  try {
    data = loadResults();
  } catch (err) {
    console.warn(`e2e summary: could not read results (${err.message}); writing fallback summary.`);
    writeFallback(err.message);
    return;
  }

  try {
    copyAssets();
    fs.mkdirSync(REPORT_DIR, { recursive: true });
    fs.writeFileSync(path.join(REPORT_DIR, 'SUMMARY.md'), renderMarkdown(data.tests));
    fs.writeFileSync(path.join(REPORT_DIR, 'summary.html'), renderHtml(data.tests));
    console.log(`e2e summary: wrote SUMMARY.md and summary.html for ${data.tests.length} test(s).`);
  } catch (err) {
    console.warn(`e2e summary: failed to render summary (${err.message}); writing fallback summary.`);
    writeFallback(err.message);
  }
}

main();
