// See the Electron documentation for details on how to use preload scripts:
// https://www.electronjs.org/docs/latest/tutorial/process-model#preload-scripts
import { ipcRenderer } from 'electron';

window.onload = async () => {
  if (window.location.protocol === 'chrome-error:') {
    await ipcRenderer.invoke('cimon-load', 'warn/unavailable');
  }
};
