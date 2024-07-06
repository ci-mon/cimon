import { app, BrowserWindow } from 'electron';
import AutoLaunch from 'auto-launch';
import { CimonApp } from './CimonApp';
import { AutoUpdater } from './auto-updater';

import log from 'electron-log';
import process from 'process';
import { registerOnSquirrelStartup } from 'node-win-toast-notifier';

import { build } from './../../package.json';
import { options } from './options';

import electron_squirrel_startup from 'electron-squirrel-startup';
import { settingsStore } from './settings';
import { initializeCimonNotifier } from './notifications/cimon-notifier-initializer';

Object.assign(console, log.functions);

let cimonApp: CimonApp | undefined = undefined;
const gotTheLock = app.requestSingleInstanceLock();
if (!gotTheLock) {
  app.quit();
} else {
  app.on('second-instance', async () => {
    await cimonApp?.onSecondInstance();
  });
}


const autoLaunch = new AutoLaunch({
  name: 'cimon',
});
autoLaunch.opts.appName = 'cimon';

if (electron_squirrel_startup) {
  const notifier = await initializeCimonNotifier();
  app.setAppUserModelId(notifier.AppId);
  await registerOnSquirrelStartup(build.appId, 'cimon desktop', options.icons.green.big_png_win);
  const cmd = process.argv[1];
  if (cmd === '--squirrel-uninstall') {
    autoLaunch.disable();
  }
  app.quit();
} else if (await settingsStore.checkBaseUrl()) {
  app.relaunch();
  app.quit();
} else {
  const notifier = await initializeCimonNotifier();
  app.setAppUserModelId(notifier.AppId);
  cimonApp = new CimonApp(settingsStore, autoLaunch, notifier);

  await AutoUpdater.install(cimonApp);

  app.on('ready', async () => {
    await cimonApp?.init();
  });
  app.on('before-quit', async () => {
    await notifier.hideAll();
  });
  app.on('window-all-closed', (e) => e.preventDefault());
  app.on('activate', async () => {
    // On OS X it's common to re-create a window in the app when the
    // dock icon is clicked and there are no other windows open.
    if (BrowserWindow.getAllWindows().length === 0) {
      await cimonApp?.showMonitors();
    }
  });
}
