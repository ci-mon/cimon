import type {ForgeConfig} from '@electron-forge/shared-types';
import {MakerSquirrel} from '@electron-forge/maker-squirrel';
import {MakerZIP} from '@electron-forge/maker-zip';
import {WebpackPlugin} from '@electron-forge/plugin-webpack';

import {mainConfig} from './webpack.main.config';
import {rendererConfig} from './webpack.renderer.config';
import {PublisherCimon} from "./publisher-cimon";
import {CimonConfig} from "./cimon-config"

const config: ForgeConfig = {
    packagerConfig: {
        icon: './icons/green/icon.ico',
        extraResource: [
            "./icons"
        ]
    },
    rebuildConfig: {},
    publishers: [new PublisherCimon({
        host: CimonConfig.url,
        token: process.env.CIMON_PUBLISH_TOKEN ?? 'changeme'
    })],
    makers: [
        new MakerSquirrel({
            setupIcon: './icons/green/icon.ico',
        }),
        new MakerZIP({}, ['darwin','linux'])
    ],
    plugins: [
        new WebpackPlugin({
            mainConfig,
            renderer: {
                config: rendererConfig,
                entryPoints: [
                    {
                        html: './src/index.html',
                        js: './src/renderer.ts',
                        name: 'main_window',
                        preload: {
                            js: './src/preload.ts',
                        },
                    },
                ],
            },
        }),
    ],
};

export default config;
