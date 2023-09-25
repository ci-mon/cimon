import Path from "path";
import isDev from "electron-is-dev";
import { CimonConfig } from "../cimon-config";
const process = require('process');

class IconLocator {
    constructor(private _basename: string) {
    }

    public get normal() {
        return `${options.resourcesPath}/icons/${this._basename}/icon.png`;
    }

    public get big_png_win() {
        return this.normal.replaceAll('/', '\\');
    }

    public get tray() {
        const iconExt = process.platform === 'win32' ? 'ico' : 'png';
        //mac resize to 16-24px
        return `${options.resourcesPath}/icons/${this._basename}/icon.${iconExt}`;
    }
}

export const options = {
    baseUrl: CimonConfig.url,
    waitForConnectionRetryDelay: 10000,

    get entrypoint() {
        return `${options.baseUrl}`;
    },
    get lastMonitor() {
        return `${options.baseUrl}/api/users/openLastMonitor?full-screen=true`;
    },
    get resourcesPath() {
        if (isDev) {
            return Path.join(__dirname, "..", "..");
        }
        return process.resourcesPath;
    },
    icons: {
        green: new IconLocator("green"),
        red: new IconLocator("red"),
    },
};
