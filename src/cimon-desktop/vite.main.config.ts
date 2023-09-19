import { defineConfig, normalizePath  } from 'vite';
import {viteStaticCopy} from "vite-plugin-static-copy";
import path from "path";

// https://vitejs.dev/config
export default defineConfig({
    build: {
        sourcemap: true
    },
  plugins: [
    viteStaticCopy({
      targets: [
        {
          src: normalizePath(path.resolve(__dirname, './node_modules/node-win-toast-notifier/bin/win-toast-notifier.exe')),
          dest: '../bin'
        }
      ]
    })
  ],
  resolve: {
    // Some libs that can run in both Web and Node.js, such as `axios`, we need to tell Vite to build them in Node.js.
    browserField: false,
    mainFields: ['module', 'jsnext:main', 'jsnext'],
  },
});
