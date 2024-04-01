// @ts-ignore esm problems.
import { MakerSquirrel } from '@electron-forge/maker-squirrel';
// @ts-ignore esm problems.
import { MakerZIP } from '@electron-forge/maker-zip';
import RemoveNodeModulesFoldersPlugin from './remove-node-modules-folders.plugin';
import PublisherCimon from './publisher-cimon';

// @ts-ignore not imported properly.
const publisherCimon = new PublisherCimon({
  host: process.env.CIMON_WEB_APP_URL ?? 'http://localhost:5001',
  token: process.env.CIMON_PUBLISH_TOKEN ?? 'changeme',
});
const config = {
  packagerConfig: {
    icon: './icons/green/icon.ico',
    extraResource: ['./icons'],
    ignore: [/^\/src/, /(.eslintrc.json)|(.gitignore)|(electron.vite.config.ts)|(forge.config.ts)|(tsconfig.*)/],
  },
  rebuildConfig: {},
  publishers: [publisherCimon],
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
