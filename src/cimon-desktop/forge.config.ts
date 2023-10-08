import type { ForgeConfig } from '@electron-forge/shared-types';
import { MakerSquirrel } from '@electron-forge/maker-squirrel';
import { MakerZIP } from '@electron-forge/maker-zip';
import { VitePlugin } from '@electron-forge/plugin-vite';
import {PublisherCimon} from "./publisher-cimon";
import {CimonConfig} from "./cimon-config";
import RemoveNodeModulesFoldersPlugin from "./remove-unneded-modules.forge.plugin";

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
      name: 'cimon',
      setupIcon: './icons/green/icon.ico',
      authors: 'Cimon'
      //remoteReleases: CimonConfig.getReleasesUrl('1.0.0')
    }),
    new MakerZIP({}, ['darwin','linux'])
  ],
  plugins: [
    new VitePlugin({
      // `build` can specify multiple entry builds, which can be Main process, Preload scripts, Worker process, etc.
      // If you are familiar with Vite configuration, it will look really familiar.
      build: [
        {
          // `entry` is just an alias for `build.lib.entry` in the corresponding file of `config`.
          entry: 'src/main.ts',
          config: 'vite.main.config.ts',
        },
        {
          entry: 'src/preload.ts',
          config: 'vite.preload.config.ts',
        },
      ],
      renderer: [
        {
          name: 'main_window',
          config: 'vite.renderer.config.ts',
        },
      ],
    }),
    new RemoveNodeModulesFoldersPlugin({
      foldersToRemove: ['prettier', 'node-win-toast-notifier', '@microsoft', 'web-streams-polyfill']
    })
  ],
};

export default config;
