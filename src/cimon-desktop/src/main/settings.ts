import Store from 'electron-store';
import { NativeAppSettings } from '../shared/interfaces';

export const settingsStore = new Store<NativeAppSettings>({
  defaults: {
    autoRun: false,
    baseUrl: import.meta.env.CIMON_WEB_APP_URL ?? 'http://localhost:5001',
  } as Readonly<NativeAppSettings>,
  schema: {
    autoRun: { type: 'boolean' },
    baseUrl: { type: 'string' },
    windowPosition: {
      type: 'object',
      properties: {
        width: {
          type: 'number',
        },
        height: {
          type: 'number',
        },
        x: {
          type: 'number',
        },
        y: {
          type: 'number',
        },
      },
    },
    screenshots: {
      type: 'object',
      properties: {
        width: {
          type: 'number',
        },
        height: {
          type: 'number',
        },
        quality: {
          type: 'number',
        },
        save: {
          type: 'boolean',
        },
        path: {
          type: 'string',
        },
      },
    },
  },
  accessPropertiesByDotNotation: true,
  watch: true,
});
