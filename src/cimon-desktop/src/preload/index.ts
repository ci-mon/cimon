// See the Electron documentation for details on how to use preload scripts:
// https://www.electronjs.org/docs/latest/tutorial/process-model#preload-scripts
import { contextBridge, ipcRenderer } from 'electron';

class Cimon {
  private _disabled: boolean = false;

  async init() {
    if (this._disabled) return;
    const baseUrl = await ipcRenderer.invoke('cimon-get-base-url');
    const result = await fetch(`${baseUrl}/auth/token`, { credentials: 'same-origin' });
    if (result.ok) {
      const tokenResponse = await result.json();
      ipcRenderer.send('cimon-token-ready', tokenResponse);
      return;
    }
    window.location.href = `${baseUrl}${this._loginPathName}`;
  }

  skipInit() {
    this._disabled = true;
  }

  private _loginPathName = '/Login';

  isLogin() {
    return window.location.pathname === this._loginPathName;
  }
}

const cimon = new Cimon();
contextBridge.exposeInMainWorld('CimonDesktop', {
  init: () => cimon.init(),
  skipInit: () => cimon.skipInit(),
});

window.onload = async () => {
  if (window.location.protocol === 'chrome-error:') {
    await ipcRenderer.invoke('cimon-load', 'warn/unavailable');
    return;
  }
  if (cimon.isLogin()) {
    return;
  }
  await cimon.init();
};
