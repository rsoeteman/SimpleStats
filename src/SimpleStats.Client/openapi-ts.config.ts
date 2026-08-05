import { defineConfig } from '@hey-api/openapi-ts';

export default defineConfig({
  input: 'http://localhost:2074/umbraco/swagger/simplestats-v1/swagger.json',
  output: 'src/api',
  plugins: [
    { name: '@hey-api/client-fetch', runtimeConfigPath: './src/hey-api.ts' },
    { name: '@hey-api/sdk', asClass: true },
  ],
});
