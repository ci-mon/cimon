// See the Electron documentation for details on how to use preload scripts:
// https://www.electronjs.org/docs/latest/tutorial/process-model#preload-scripts
import {contextBridge, ipcRenderer} from "electron";

class Cimon {
    private _disabled: boolean;
    async init() {
        if (this._disabled) return;
        const baseUrl = await ipcRenderer.invoke('cimon-get-base-url');
        const tokenUrl = `${baseUrl}/auth/token`;
        const result = await fetch(tokenUrl);
        if (result.ok) {
            console.log('TOKEN !!!!!!!!!!', result.ok);
            const tokenResponse = await result.json();
            await ipcRenderer.send('cimon-token-ready', tokenResponse);
            return;
        }
        window.location.href = tokenUrl;
    }
    skipInit(){
        this._disabled = true;
    }
}
const cimon = new Cimon();
contextBridge.exposeInMainWorld('CimonDesktop', {
    init: () => cimon.init(),
    skipInit: () => cimon.skipInit(),
});

window.onload = async () => {
    await cimon.init();
};

(async ()=>{
    if (window.location.protocol === 'chrome-error:') {
        await ipcRenderer.invoke('cimon-app-show-warn', 'unavailable');
        return
    }
    window.localStorage.setItem('SidebarCollapsed', 'true');
})();

