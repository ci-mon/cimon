import { NativeAppSettings } from '../shared/interfaces';

export interface Result {
  error?: { message: string }
}

export interface IElectronAPI {
  selectFolder: (path?: string) => Promise<string>;
  getOptions: () => Promise<NativeAppSettings>;
  saveOptions: (options: NativeAppSettings) => Promise<Result>;
  trySetBaseUrl: (url: string) => Promise<Result>;
  setOverlay:(svg: string) => Promise<void>;
}
