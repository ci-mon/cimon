import {app, BrowserWindow} from "electron";
// This allows TypeScript to pick up the magic constants that's auto-generated by Forge's Webpack
// plugin that tells the Electron app where to look for the Webpack-bundled app code (depending on
// whether you're running in development or production).
import {CimonApp} from "./CimonApp";
import {AutoUpdater} from "./auto-updater";

import log from "electron-log";
Object.assign(console, log.functions);

// Handle creating/removing shortcuts on Windows when installing/uninstalling.
if (require("electron-squirrel-startup")) {
  app.quit();
}

app.setAppUserModelId("cimon.desktop.app");
const cimonApp = new CimonApp();

AutoUpdater.install(cimonApp);

app.on("ready", async () => {
  await cimonApp.init();
});

app.on("activate", async () => {
  // On OS X it's common to re-create a window in the app when the
  // dock icon is clicked and there are no other windows open.
  if (BrowserWindow.getAllWindows().length === 0) {
    await cimonApp.showMonitors();
  }
});
