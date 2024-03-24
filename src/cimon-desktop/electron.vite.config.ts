import path, { resolve } from 'path';
import { externalizeDepsPlugin } from 'electron-vite';
import vue from '@vitejs/plugin-vue';
import { defineConfig, ResolvedConfig, Plugin, UserConfig } from 'vite';
import fs from 'fs';

import { build } from './package.json';

let viteConfig: ResolvedConfig | undefined = undefined,
  notifierCopied: boolean;
const copyNotifierPlugin = {
  name: 'copy-notifier',
  configResolved(config) {
    viteConfig = config;
  },
  async writeBundle() {
    if (!notifierCopied) {
      notifierCopied = true;
      const source = path.resolve(__dirname, `./node_modules/node-win-toast-notifier/bin/win-toast-notifier.exe`);
      const dest = path.resolve(viteConfig!.build.outDir, '../bin');
      if (!fs.existsSync(dest)) {
        fs.mkdirSync(dest);
      }
      fs.copyFileSync(source, path.resolve(dest, build.notifier_exe_name));
      console.info('cimon-desktop-notifier.exe copied!');
    }
    return Promise.resolve(null);
  },
} as unknown as Plugin;
export default defineConfig(
  ({ mode }) =>
    ({
      main: {
        build: {
          outDir: 'dist/main',
          sourcemap: true,
        },
        plugins: [externalizeDepsPlugin(), copyNotifierPlugin],
      },
      preload: {
        build: {
          outDir: 'dist/preload',
          rollupOptions: {
            input: {
              cimon: resolve(__dirname, 'src/preload/index.ts'),
              internal: resolve(__dirname, 'src/internal-preload/index.ts'),
            },
            output: {
              format: 'cjs',
            },
          },
        },
        plugins: [externalizeDepsPlugin()],
      },
      renderer: {
        build: { outDir: 'dist/renderer', target: 'es2020' },
        resolve: {
          alias: {
            '@renderer': resolve('src/renderer/src'),
          },
        },
        plugins: [vue({ isProduction: mode === 'build' })],
      },
    }) as UserConfig
);
