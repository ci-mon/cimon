import { app, autoUpdater, dialog } from 'electron';
import log from 'electron-log';
import { CimonApp } from './CimonApp';
import { arch, platform } from 'process';
import { settingsStore } from './settings';

const updaterLog = log.create({ logId: 'update' });

export class AutoUpdater {
  private static async getReleasesUrl(version: string) {
    const baseUrl = await settingsStore.getBaseUrl();
    const callbackUrl = Buffer.from(baseUrl).toString('base64');
    return `${baseUrl}/native/update/${callbackUrl}/${platform}/${arch}/${version}`;
  }
  public static async install(cimonApp: CimonApp) {
    const feedUrl = await AutoUpdater.getReleasesUrl(app.getVersion());
    updaterLog.info(`Updater feed url: ${feedUrl}`);
    if (!app.isPackaged) {
      return;
    }
    autoUpdater.setFeedURL({ url: feedUrl });
    settingsStore.onDidChange('baseUrl', async () => {
      const feedUrl = await AutoUpdater.getReleasesUrl(app.getVersion());
      autoUpdater.setFeedURL({ url: feedUrl });
    });

    autoUpdater.on('update-downloaded', () => {
      cimonApp.setUpdateReady();
      dialog
        .showMessageBox({
          type: 'info',
          buttons: ['Restart', 'Later'],
          title: 'Cimon',
          message: 'application update',
          detail: 'A new version has been downloaded. Restart the application to apply the updates.',
        })
        .then((returnValue) => {
          if (returnValue.response === 0) {
            cimonApp.quitAndUpdate();
          } else updaterLog.info('Update postponed');
        });
    });
    autoUpdater.on('error', (message) => {
      updaterLog.error('There was a problem updating the application');
      updaterLog.error(message);
    });
    setInterval(() => {
      autoUpdater.checkForUpdates();
    }, 60000);
  }

  public static checkForUpdates() {
    autoUpdater.checkForUpdates();
  }
}
