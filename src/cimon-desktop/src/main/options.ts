import Path from 'path';
import isDev from 'electron-is-dev';
import { CimonConfig } from '../../cimon-config';
import { resourcesPath, platform } from 'process';

class IconLocator {
  constructor(private _basename: string) {}

  public get normal() {
    return `${options.resourcesPath}/icons/${this._basename}/icon.png`;
  }

  public get big_png_win() {
    return this.normal.replaceAll('/', '\\');
  }

  public get tray() {
    const iconExt = platform === 'win32' ? 'ico' : 'png';
    //mac resize to 16-24px
    return `${options.resourcesPath}/icons/${this._basename}/icon.${iconExt}`;
  }
}

export const options = {
  baseUrl: CimonConfig.url,
  waitForConnectionRetryDelay: 10000,

  monitorUrl(key: string): string {
    return this.baseUrl + `/monitor/${key}?full-screen=true`;
  },
  discussionUrl(urlPart: string) {
    const url = new URL(this.baseUrl + urlPart);
    url.searchParams.append('full-screen', 'true');
    return url.href;
  },
  get entrypoint() {
    return `${this.baseUrl}`;
  },
  get tokenUrl() {
    return `${this.baseUrl}/auth/token`;
  },
  get autologin() {
    return `${this.baseUrl}/auth/autologin`;
  },
  get lastMonitor() {
    return `${this.baseUrl}/api/users/openLastMonitor?full-screen=true`;
  },
  get discussionWindowUrl() {
    return `${this.baseUrl}/buildDiscussion/`;
  },
  get loginPageUrl() {
    return `${this.baseUrl}/Login`;
  },
  isLoginPage(url) {
    if (!url) {
      return false;
    }
    const path = new URL(url).pathname;
    return path.toLowerCase() === '/login';
  },
  get resourcesPath() {
    if (isDev) {
      return Path.join(__dirname, '..', '..');
    }
    return resourcesPath;
  },
  icons: {
    green: new IconLocator('green'),
    red: new IconLocator('red'),
  },
};
