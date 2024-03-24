import { NativeAppSettings } from '../shared/interfaces';

export interface IElectronAPI {
  selectFolder: (path?: string) => Promise<string>;
  getOptions: () => Promise<NativeAppSettings>;
  saveOptions: (options: NativeAppSettings) => Promise<{ error?: { message: string } }>;
}
