
const dashboards = [
    {
        type: 'dashboard',
        name: 'simplestats dashboard',
        alias: 'simplestats.dashboard',
        elementName: 'simplestats-dashboard',
        js: () => import("./simplestats-dashboard.element.ts"),
        weight: -10,
        meta: {
            label: '#simpleStats_dashboardLabel',
            pathname: 'simplestatsdashboard'
        },
        conditions: [
            {
                alias: 'Umb.Condition.SectionAlias',
                match: 'Umb.Section.Content'
            }
        ]
    }
]

export const manifests = [...dashboards];