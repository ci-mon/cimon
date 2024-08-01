import Store from 'electron-store';
import { NativeAppSettings } from '../shared/interfaces';
export class CimonSettingsStore extends Store<NativeAppSettings> {
  async checkBaseUrl() {
    try {
      const resp = await fetch(`${this.store.baseUrl}`, { redirect: 'manual' });
      if (resp.redirected || resp.status === 301) {
        return await this._handleRedirection(resp);
      }
    } catch {
      // ignored
    }
    return false;
  }

  private async _handleRedirection(resp: Response) {
    const newUrl = resp.headers.get('location');
    try {
      if (newUrl) {
        const response = await fetch(`${newUrl}/info`);
        if (response?.ok) {
          const json = await response.json();
          if (!json?.version) {
            return false;
          }
          this.set('baseUrl', newUrl);
          return true;
        }
      }
    } catch (e) {
      console.warn(`Base url was redirected to ${newUrl} but it not works: ${(e as Error)?.message}`);
    }
    return false;
  }
}
export const settingsStore = new CimonSettingsStore({
  defaults: {
    autoRun: false,
    baseUrl: import.meta.env.CIMON_WEB_APP_URL ?? 'http://localhost:5001',
  } as Readonly<NativeAppSettings>,
  schema: {
    hideWhenMinimized: { type: 'boolean' },
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
