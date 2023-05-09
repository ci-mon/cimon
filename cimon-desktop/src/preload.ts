// See the Electron documentation for details on how to use preload scripts:
// https://www.electronjs.org/docs/latest/tutorial/process-model#preload-scripts
import {contextBridge, ipcRenderer} from "electron";

window.localStorage.setItem('SidebarCollapsed', 'true');
const cimon = {
    init: async () => {
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
}
contextBridge.exposeInMainWorld('CimonDesktop', cimon);

(async function (){
    await cimon.init()
})();