// SimpleStats backoffice extension entry point.
// All Umbraco extension manifests are registered here.
import './dashboard/simplestats-dashboard.element.js';

const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'dashboard',
    alias: 'SimpleStats.Dashboard',
    name: 'SimpleStats Dashboard',
    element: () => import('./dashboard/simplestats-dashboard.element.js'),
    meta: {
      label: 'SimpleStats',
      pathname: 'simplestats',
    },
    conditions: [
      {
        alias: 'Umb.Condition.SectionAlias',
        match: 'Umb.Section.Content',
      },
    ],
  },
];

export { manifests };
