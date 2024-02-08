import { app, autoUpdater, dialog } from 'electron';
import log from 'electron-log';
import { CimonConfig } from '../../cimon-config';
import { CimonApp } from './CimonApp';

const updaterLog = log.create({ logId: 'update' });

export class AutoUpdater {
  public static install(cimonApp: CimonApp) {
    const feedUrl = CimonConfig.getReleasesUrl(app.getVersion());
    updaterLog.info(`Updater feed url: ${feedUrl}`);
    if (!app.isPackaged) {
      return;
    }
    autoUpdater.setFeedURL({ url: feedUrl });

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
          if (returnValue.response === 0) autoUpdater.quitAndInstall();
          else updaterLog.info('Update postponed');
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
}
