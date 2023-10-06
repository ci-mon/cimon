import {build} from "./package.json"

import {defineConfig, Plugin, ResolvedConfig} from 'vite';
import path from "path";
import fs from "fs";


// https://vitejs.dev/config
export default defineConfig(async () => {

    let viteConfig: ResolvedConfig = null, notifierCopied: boolean;
    return {
        build: {
            sourcemap: true
        },
        plugins: [
            {
                name: 'copy-notifier',
                configResolved(config) {
                    viteConfig = config;
                },
                async writeBundle(){
                    if (!notifierCopied) {
                        notifierCopied = true;
                        let source = path.resolve(__dirname, `./node_modules/node-win-toast-notifier/bin/win-toast-notifier.exe`);
                        let dest = path.resolve(viteConfig.build.outDir, '../bin');
                        fs.mkdirSync(dest);
                        fs.copyFileSync(source, path.resolve(dest, build.notifier_exe_name));
                        console.info('cimon-desktop-notifier.exe copied!')
                    }
                    return Promise.resolve(null);
                }
            } as Plugin
        ],
        resolve: {
            browserField: false,
            mainFields: ['module', 'jsnext:main', 'jsnext'],
        },
    }
});
