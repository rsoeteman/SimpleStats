import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vite';

export default defineConfig({
  resolve: {
    alias: {
      '@simplestats-dashboards': fileURLToPath(new URL('./src/dashboards', import.meta.url)),
      '@simplestats-languages': fileURLToPath(new URL('./src/lang', import.meta.url)),
    },
  },
  build: {
    lib: {
      entry: 'src/index.ts',
      formats: ['es'],
      fileName: () => 'simplestats.js',
    },
    outDir: 'wwwroot/App_Plugins/SimpleStats/assets',
    emptyOutDir: true,
    rollupOptions: {
      external: [/^@umbraco-cms\/.*/],
    },
  },
});
