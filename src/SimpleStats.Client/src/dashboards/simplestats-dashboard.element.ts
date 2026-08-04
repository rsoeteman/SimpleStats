import { html, css, customElement } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';

/**
 * SimpleStats dashboard element – renders site statistics in the Umbraco backoffice.
 */
@customElement('simplestats-dashboard')
export class SimpleStatsDashboardElement extends UmbLitElement {
  static override styles = css`
    :host {
      display: block;
      padding: var(--uui-size-space-5);
    }

    h2 {
      margin-top: 0;
    }
  `;

  override render() {
    return html`
      <uui-box>
        <h2>SimpleStats</h2>
        <p>Welcome to SimpleStats. Statistics will appear here once data is collected.</p>
      </uui-box>
    `;
  }
}

export default SimpleStatsDashboardElement;
