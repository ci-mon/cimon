import {app, BrowserWindow} from "electron";
import {CimonApp} from "./CimonApp";
import {AutoUpdater} from "./auto-updater";

import log from "electron-log";
import isDev from "electron-is-dev";
import process from "process";
Object.assign(console, log.functions);

// Handle creating/removing shortcuts on Windows when installing/uninstalling.
if (require("electron-squirrel-startup")) {
    app.quit();
}

if (isDev) {
    // https://www.electronjs.org/docs/latest/tutorial/notifications#windows
    app.setAppUserModelId(process.execPath);
}
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
