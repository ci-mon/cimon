import { app, BrowserWindow, session, ipcMain, Tray, Menu, MenuItem  } from 'electron';
import {rendererConfig} from "../webpack.renderer.config";
// This allows TypeScript to pick up the magic constants that's auto-generated by Forge's Webpack
// plugin that tells the Electron app where to look for the Webpack-bundled app code (depending on
// whether you're running in development or production).
import {
  tapAfterEnvironmentToPatchWatching
} from "fork-ts-checker-webpack-plugin/lib/hooks/tap-after-environment-to-patch-watching";
import {CimonApp} from "./CimonApp";

// Handle creating/removing shortcuts on Windows when installing/uninstalling.
if (require('electron-squirrel-startup')) {
  app.quit();
}
const cimonApp = new CimonApp();
const onReady = async () => {
  await cimonApp.init();
};

// This method will be called when Electron has finished
// initialization and is ready to create browser windows.
// Some APIs can only be used after this event occurs.
app.on('ready', onReady);

app.on('activate', async () => {
  // On OS X it's common to re-create a window in the app when the
  // dock icon is clicked and there are no other windows open.
  if (BrowserWindow.getAllWindows().length === 0) {
    await cimonApp.showMonitors();
  }
});
