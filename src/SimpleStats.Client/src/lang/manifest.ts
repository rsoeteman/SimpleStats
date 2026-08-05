
const languages = [
    {
        type: 'localization',
        alias: 'SimpleStats.Localization.En',
        name: 'English (en) SimpleStats Localization',
        js: () => import("./en.ts"),
        weight: 1,
        meta: {
            culture: 'en'
        }
    }
]

export const manifests = [...languages];
