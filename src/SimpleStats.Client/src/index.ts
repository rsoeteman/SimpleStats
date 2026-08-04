import {UmbEntryPointOnInit} from '@umbraco-cms/backoffice/extension-api';
import {manifests as dashboardManifests} from '@simplestats-dashboards/manifest.ts';
import {UMB_AUTH_CONTEXT} from "@umbraco-cms/backoffice/auth";

export const onInit: UmbEntryPointOnInit = (_host, extensionRegistry) => {

    console.log('Registering SimpleStats manifests');
    extensionRegistry.registerMany([
        ...dashboardManifests,
    ]);


    _host.consumeContext(UMB_AUTH_CONTEXT, (_auth) => {
        if (!_auth) {
            return;
        }
    });

};