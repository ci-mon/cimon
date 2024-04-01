export interface NativeAppSettings {
  windowPosition: Electron.Rectangle;
  screenshots: ScreenshotOptions;
  autoRun: boolean;
  baseUrl: string;
}

export interface ScreenshotOptions {
  width: number;
  height: number;
  quality: number;
  save: boolean;
  path?: string;
}
