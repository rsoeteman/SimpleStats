import { html, css, nothing, customElement, state, repeat } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';

const PAGE_SIZE = 50;

const NODE_NAMES = [
  'Home', 'About Us', 'Contact', 'News', 'News/Company Update', 'News/Product Launch',
  'News/Press Release', 'Blog', 'Blog/Getting Started', 'Blog/Release Notes',
  'Blog/Case Study Alpha', 'Blog/Case Study Beta', 'Products', 'Products/Widget A',
  'Products/Widget B', 'Products/Widget C', 'Products/Widget D', 'Products/Accessories',
  'Services', 'Services/Consulting', 'Services/Support', 'Services/Training', 'Team',
  'Team/Leadership', 'Team/Careers', 'Careers/Open Roles', 'Pricing', 'Pricing/Enterprise',
  'Pricing/Starter', 'FAQ', 'Support', 'Support/Knowledge Base', 'Support/Contact Support',
  'Legal', 'Legal/Privacy Policy', 'Legal/Terms of Service', 'Partners', 'Partners/Directory',
  'Events', 'Events/Webinar Series', 'Events/Conference 2025', 'Downloads',
  'Downloads/Whitepapers', 'Downloads/Datasheets', 'Locations', 'Locations/Amsterdam',
  'Locations/Berlin', 'Locations/London', 'Media Gallery', 'Testimonials', 'Case Studies',
];

interface ContentAccessNode {
  id: number;
  name: string;
  events: number[];
}

interface ContentAccessRow {
  id: number;
  name: string;
  count: number;
}

function createSeededRandom(seed: number) {
  let value = seed;
  return () => {
    value = (value * 1103515245 + 12345) & 0x7fffffff;
    return value / 0x7fffffff;
  };
}

/** Placeholder access data until a real reporting API is wired up. */
function generateMockNodes(): ContentAccessNode[] {
  const random = createSeededRandom(42);
  const now = Date.now();
  const oneYearMs = 365 * 24 * 60 * 60 * 1000;
  return NODE_NAMES.map((name, id) => {
    const eventCount = Math.floor(random() * 40) * (random() > 0.15 ? 1 : 0);
    const events: number[] = [];
    for (let i = 0; i < eventCount; i++) {
      events.push(now - Math.floor(random() * oneYearMs * 1.3));
    }
    return { id, name, events };
  });
}

const MOCK_NODES = generateMockNodes();

function toISODate(date: Date): string {
  return date.toISOString().slice(0, 10);
}

function defaultStartDate(): Date {
  const date = new Date();
  date.setFullYear(date.getFullYear() - 1);
  return date;
}

/**
 * SimpleStats dashboard element – shows a Content Access Report in the Umbraco backoffice.
 */
@customElement('simplestats-dashboard')
export class SimpleStatsDashboardElement extends UmbLitElement {
  @state()
  private _startDate = toISODate(defaultStartDate());

  @state()
  private _endDate = toISODate(new Date());

  @state()
  private _appliedRange = { start: this._startDate, end: this._endDate };

  @state()
  private _page = 0;

  #getRows(): ContentAccessRow[] {
    const startMs = new Date(`${this._appliedRange.start}T00:00:00`).getTime();
    const endMs = new Date(`${this._appliedRange.end}T23:59:59`).getTime();
    return MOCK_NODES.map((node) => ({
      id: node.id,
      name: node.name,
      count: node.events.filter((ts) => ts >= startMs && ts <= endMs).length,
    }))
      .filter((row) => row.count > 0)
      .sort((a, b) => b.count - a.count);
  }

  #onStartDateChange(event: Event) {
    this._startDate = (event.target as HTMLInputElement).value;
  }

  #onEndDateChange(event: Event) {
    this._endDate = (event.target as HTMLInputElement).value;
  }

  #applyFilter() {
    this._appliedRange = { start: this._startDate, end: this._endDate };
    this._page = 0;
  }

  #onPageChange(event: Event) {
    this._page = (event.target as HTMLElement & { current: number }).current - 1;
  }

  override render() {
    const rangeInvalid = new Date(this._startDate) > new Date(this._endDate);
    const rows = this.#getRows();
    const pageCount = Math.max(1, Math.ceil(rows.length / PAGE_SIZE));
    const page = Math.min(this._page, pageCount - 1);
    const pagedRows = rows.slice(page * PAGE_SIZE, page * PAGE_SIZE + PAGE_SIZE);

    return html`
      <uui-box headline="Content Access Report">
        <p class="description">
          Shows how many times each content node was accessed within the selected date range.
        </p>

        <div class="filters">
          <div class="field">
            <label class="field-label" for="start-date">Start date</label>
            <uui-input id="start-date" type="date" .value=${this._startDate} @change=${this.#onStartDateChange}></uui-input>
          </div>
          <div class="field">
            <label class="field-label" for="end-date">End date</label>
            <uui-input id="end-date" type="date" .value=${this._endDate} @change=${this.#onEndDateChange}></uui-input>
          </div>
          <uui-button look="primary" label="Apply" ?disabled=${rangeInvalid} @click=${this.#applyFilter}>
            Apply
          </uui-button>
        </div>

        ${rangeInvalid ? html`<p class="error">Start date must be before end date.</p>` : nothing}

        ${rows.length === 0 ? this.#renderEmptyState() : this.#renderTable(rows, pagedRows, page, pageCount)}
      </uui-box>
    `;
  }

  #renderEmptyState() {
    return html`
      <div class="empty-state">
        <div class="empty-title">No records found</div>
        <div class="empty-description">
          No content nodes were accessed between ${this._appliedRange.start} and ${this._appliedRange.end}.
          Try widening the date range.
        </div>
      </div>
    `;
  }

  #renderTable(rows: ContentAccessRow[], pagedRows: ContentAccessRow[], page: number, pageCount: number) {
    return html`
      <uui-table aria-label="Content access report">
        <uui-table-head>
          <uui-table-head-cell>Node Name</uui-table-head-cell>
          <uui-table-head-cell>Times Accessed</uui-table-head-cell>
        </uui-table-head>
        ${repeat(
          pagedRows,
          (row) => row.id,
          (row) => html`
            <uui-table-row>
              <uui-table-cell>${row.name}</uui-table-cell>
              <uui-table-cell class="count-cell">${row.count.toLocaleString()}</uui-table-cell>
            </uui-table-row>
          `,
        )}
      </uui-table>

      <div class="pagination">
        <div class="pagination-summary">
          Showing ${page * PAGE_SIZE + 1}–${Math.min(rows.length, (page + 1) * PAGE_SIZE)} of ${rows.length} nodes
          · ${PAGE_SIZE} per page
        </div>
        <uui-pagination .total=${pageCount} .current=${page + 1} @change=${this.#onPageChange}></uui-pagination>
      </div>
    `;
  }

  static override styles = css`
    :host {
      display: block;
      padding: var(--uui-size-space-5);
    }

    .description {
      font-size: 14px;
      line-height: 1.6;
      margin: 0 0 18px;
      color: var(--uui-color-text-alt);
    }

    .filters {
      display: flex;
      align-items: flex-end;
      gap: 12px;
      margin-bottom: 20px;
      flex-wrap: wrap;
    }

    .field {
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .field-label {
      font-size: 12px;
      font-weight: 700;
      color: var(--uui-color-text-alt);
    }

    .field uui-input {
      width: 170px;
    }

    .error {
      font-size: 13px;
      color: var(--uui-color-danger);
      margin: -8px 0 18px;
    }

    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      text-align: center;
      padding: 56px 20px;
      border: 1px dashed var(--uui-color-divider-standalone);
      border-radius: 15px;
      color: var(--uui-color-text-alt);
    }

    .empty-title {
      font-size: 15px;
      font-weight: 700;
      color: var(--uui-color-text);
      margin-bottom: 6px;
    }

    .empty-description {
      font-size: 14px;
      max-width: 360px;
    }

    uui-table {
      width: 100%;
    }

    .count-cell {
      font-variant-numeric: tabular-nums;
    }

    .pagination {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-top: 16px;
      font-size: 13px;
      color: var(--uui-color-text-alt);
      flex-wrap: wrap;
      gap: 12px;
    }
  `;
}

export default SimpleStatsDashboardElement;
