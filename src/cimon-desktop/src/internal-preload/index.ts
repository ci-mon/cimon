import { contextBridge, ipcRenderer } from 'electron';
import { IElectronAPI } from './index.d';
import { NativeAppSettings } from '../shared/interfaces';

class ElectronAPI implements IElectronAPI {
  public async saveOptions(options: NativeAppSettings) {
    return await ipcRenderer.invoke('options:save', options);
  }

  public selectFolder(currentPath?: string) {
    return ipcRenderer.invoke('dialog:selectDir', currentPath);
  }
  public async getOptions() {
    return await ipcRenderer.invoke('options:read');
  }
}

const keys = (x) => Object.getOwnPropertyNames(x).concat(Object.getOwnPropertyNames(x?.__proto__));
const isObject = (v) => Object.prototype.toString.call(v) === '[object Object]';

const classToObject = (clss) =>
  keys(clss ?? {}).reduce((object, key) => {
    const [val, arr, obj] = [clss[key], Array.isArray(clss[key]), isObject(clss[key])];
    object[key] = arr ? val.map(classToObject) : obj ? classToObject(val) : val;
    return object;
  }, {});

const instance = new ElectronAPI();
const api = classToObject(instance);
contextBridge.exposeInMainWorld('electronAPI', api);
