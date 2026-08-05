import { html, css, nothing, customElement, state, repeat } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { SimpleStatsPageViewsRepository } from './repository/page-views.repository.ts';

const PAGE_SIZE = 50;

interface ContentAccessRow {
  id: string;
  name: string;
  count: number;
}

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

  @state()
  private _rows: ContentAccessRow[] = [];

  @state()
  private _loading = false;

  @state()
  private _error = false;

  #repository = new SimpleStatsPageViewsRepository(this);

  override connectedCallback() {
    super.connectedCallback();
    this.#fetchRows();
  }

  async #fetchRows() {
    this._loading = true;
    this._error = false;

    try {
      const { data, error } = await this.#repository.requestPageViews(this._appliedRange.start, this._appliedRange.end);

      if (error) {
        console.error('[SimpleStats] Failed to load page views', error);
        this._error = true;
        this._rows = [];
      } else {
        this._rows = (data ?? [])
          .map((summary) => ({ id: summary.nodeKey, name: summary.nodeName, count: summary.count }))
          .sort((a, b) => b.count - a.count);
      }
    } catch (error) {
      console.error('[SimpleStats] Unexpected error loading page views', error);
      this._error = true;
      this._rows = [];
    } finally {
      this._loading = false;
    }
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
    this.#fetchRows();
  }

  #onPageChange(event: Event) {
    this._page = (event.target as HTMLElement & { current: number }).current - 1;
  }

  override render() {
    const rangeInvalid = new Date(this._startDate) > new Date(this._endDate);
    const rows = this._rows;
    const pageCount = Math.max(1, Math.ceil(rows.length / PAGE_SIZE));
    const page = Math.min(this._page, pageCount - 1);
    const pagedRows = rows.slice(page * PAGE_SIZE, page * PAGE_SIZE + PAGE_SIZE);

    return html`
      <uui-box headline=${this.localize.term('simpleStats_reportHeadline')}>
        <p class="description">
          ${this.localize.term('simpleStats_reportDescription')}
        </p>

        <div class="filters">
          <div class="field">
            <label class="field-label" for="start-date">${this.localize.term('simpleStats_startDate')}</label>
            <uui-input id="start-date" type="date" .value=${this._startDate} @change=${this.#onStartDateChange}></uui-input>
          </div>
          <div class="field">
            <label class="field-label" for="end-date">${this.localize.term('simpleStats_endDate')}</label>
            <uui-input id="end-date" type="date" .value=${this._endDate} @change=${this.#onEndDateChange}></uui-input>
          </div>
          <uui-button look="primary" label=${this.localize.term('simpleStats_apply')} ?disabled=${rangeInvalid} @click=${this.#applyFilter}>
            ${this.localize.term('simpleStats_apply')}
          </uui-button>
        </div>

        ${rangeInvalid ? html`<p class="error">${this.localize.term('simpleStats_dateRangeError')}</p>` : nothing}

        ${this._loading
          ? this.#renderLoading()
          : this._error
            ? this.#renderErrorState()
            : rows.length === 0
              ? this.#renderEmptyState()
              : this.#renderTable(rows, pagedRows, page, pageCount)}
      </uui-box>
    `;
  }

  #renderLoading() {
    return html`
      <div class="loading">
        <uui-loader></uui-loader>
      </div>
    `;
  }

  #renderErrorState() {
    return html`
      <div class="empty-state">
        <div class="empty-title">${this.localize.term('simpleStats_errorTitle')}</div>
        <div class="empty-description">${this.localize.term('simpleStats_errorDescription')}</div>
      </div>
    `;
  }

  #renderEmptyState() {
    return html`
      <div class="empty-state">
        <div class="empty-title">${this.localize.term('simpleStats_emptyTitle')}</div>
        <div class="empty-description">
          ${this.localize.term('simpleStats_emptyDescription', this._appliedRange.start, this._appliedRange.end)}
        </div>
      </div>
    `;
  }

  #renderTable(rows: ContentAccessRow[], pagedRows: ContentAccessRow[], page: number, pageCount: number) {
    return html`
      <uui-table aria-label=${this.localize.term('simpleStats_tableAriaLabel')}>
        <uui-table-head>
          <uui-table-head-cell>${this.localize.term('simpleStats_columnNodeName')}</uui-table-head-cell>
          <uui-table-head-cell>${this.localize.term('simpleStats_columnTimesAccessed')}</uui-table-head-cell>
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
          ${this.localize.term(
            'simpleStats_paginationSummary',
            page * PAGE_SIZE + 1,
            Math.min(rows.length, (page + 1) * PAGE_SIZE),
            rows.length,
            PAGE_SIZE,
          )}
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

    .loading {
      display: flex;
      justify-content: center;
      padding: 56px 20px;
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
