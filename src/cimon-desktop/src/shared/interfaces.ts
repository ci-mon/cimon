export interface NativeAppSettings {
  screenshots: ScreenshotOptions;
  autoRun: boolean;
}

export interface ScreenshotOptions {
  width: number;
  height: number;
  quality: number;
  save: boolean;
  path?: string;
}
