import { app, BrowserWindow } from 'electron';
import AutoLaunch from 'auto-launch';
import { CimonApp } from './CimonApp';
import { AutoUpdater } from './auto-updater';

import log from 'electron-log';
import isDev from 'electron-is-dev';
import process from 'process';
import { registerOnSquirrelStartup, Notifier, registerAppId } from 'node-win-toast-notifier';

import { build } from './../../package.json';
import { options } from './options';
import { NotifierWrapper } from './notifierWrapper';

import electron_squirrel_startup from 'electron-squirrel-startup';
import { settingsStore } from './settings';

Object.assign(console, log.functions);

Notifier.ExecutableName = build.notifier_exe_name;

const autoLaunch = new AutoLaunch({
  name: 'cimon',
});
autoLaunch.opts.appName = 'cimon';

if (electron_squirrel_startup) {
  await registerOnSquirrelStartup(build.appId, 'cimon desktop', options.icons.green.big_png_win);
  const cmd = process.argv[1];
  if (cmd === '--squirrel-updated') {
    const isAutorunEnabled = await autoLaunch.isEnabled();
    if (settingsStore.store.autoRun && !isAutorunEnabled) {
      autoLaunch.enable();
    }
  } else if (cmd === '--squirrel-uninstall') {
    autoLaunch.disable();
  }
  app.quit();
}
if (isDev) {
  NotifierWrapper.AppId = process.execPath;
  app.setAppUserModelId(NotifierWrapper.AppId);
  // if notifications not visible uncomment this and run once
  // await unRegisterAppId(NotifierWrapper.AppId);
  await registerAppId(NotifierWrapper.AppId);
} else {
  app.setAppUserModelId(build.appId);
}
const cimonApp = new CimonApp(settingsStore, autoLaunch);

AutoUpdater.install(cimonApp);

app.on('ready', async () => {
  await cimonApp.init();
});
app.on('window-all-closed', (e) => e.preventDefault());
app.on('activate', async () => {
  // On OS X it's common to re-create a window in the app when the
  // dock icon is clicked and there are no other windows open.
  if (BrowserWindow.getAllWindows().length === 0) {
    await cimonApp.showMonitors();
  }
});
