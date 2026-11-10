import { defineConfig } from '@hey-api/openapi-ts';

export default defineConfig({
  input: 'http://localhost:2074/umbraco/openapi/simplestats-v1.json',
  output: 'src/api',
  plugins: [
    { name: '@hey-api/client-fetch', runtimeConfigPath: './src/hey-api.ts' },
    { name: '@hey-api/sdk', asClass: true },
  ],
});
