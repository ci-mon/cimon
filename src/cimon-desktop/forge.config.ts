import { MakerSquirrel } from '@electron-forge/maker-squirrel';
import { MakerZIP } from '@electron-forge/maker-zip';
import { PublisherCimon } from './publisher-cimon';
import { CimonConfig } from './cimon-config';
import RemoveNodeModulesFoldersPlugin from './remove-unneded-modules.forge.plugin';

const config = {
  packagerConfig: {
    icon: './icons/green/icon.ico',
    extraResource: ['./icons'],
    ignore: [/^\/src/, /(.eslintrc.json)|(.gitignore)|(electron.vite.config.ts)|(forge.config.ts)|(tsconfig.*)/],
  },
  rebuildConfig: {},
  publishers: [
    new PublisherCimon({
      host: CimonConfig.url,
      token: process.env.CIMON_PUBLISH_TOKEN ?? 'changeme',
    }),
  ],
  makers: [
    new MakerSquirrel(
      {
        name: 'cimon',
        setupIcon: './icons/green/icon.ico',
        authors: 'Cimon',
        //remoteReleases: CimonConfig.getReleasesUrl('1.0.0')
      },
      ['win32']
    ),
    new MakerZIP({}, ['darwin', 'linux']),
  ],
  plugins: [
    new RemoveNodeModulesFoldersPlugin({
      foldersToRemove: ['prettier', 'node-win-toast-notifier', '@microsoft', 'web-streams-polyfill', '.vite'],
    }),
  ],
};
export default config;
