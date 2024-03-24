import {
  app,
  autoUpdater,
  BrowserWindow,
  ipcMain,
  IpcMainEvent,
  Menu,
  MenuItemConstructorOptions,
  net,
  session,
  shell,
  Tray,
  WebContents,
  dialog,
} from 'electron';
import { ConnectionState, MonitorInfo, SignalRClient } from './SignalRClient';
import log from 'electron-log';
import isDev from 'electron-is-dev';
import { NotifierWrapper } from './notifierWrapper';
import path from 'path';
import { options } from './options';

import process from 'process';
import Store from 'electron-store';
import AutoLaunch from 'auto-launch';
import { NativeAppSettings } from '../shared/interfaces';
import fs from 'fs';

interface MentionInfo {
  buildConfigId: number;
  commentsCount: number;
  buildConfigKey: string;
}
type TokenInfo = {
  userName: string;
  token: string;
};
type PromiseInfo<T> = {
  resolve: (value: T) => void;
  reject: () => void;
};

type TokenDataReceiver = PromiseInfo<TokenInfo>;

export class CimonApp {
  private _window!: Electron.CrossProcessExports.BrowserWindow;
  private _discussionWindow?: Electron.CrossProcessExports.BrowserWindow;
  private _loginWindow?: Electron.CrossProcessExports.BrowserWindow;
  private _optionsWindow?: Electron.CrossProcessExports.BrowserWindow;

  private tokenDataReceiver?: TokenDataReceiver;
  private _mentions: MentionInfo[] = [];
  private _monitorInfo?: MonitorInfo;
  private _updateReady = false;
  private _isExiting = false;
  private _tray!: Tray;
  private _trayContextMenu!: Electron.Menu;
  private _session!: Electron.Session;
  private _signalR!: SignalRClient;
  private _autoLaunch: AutoLaunch;
  private _store: Store<NativeAppSettings> = new Store<NativeAppSettings>({
    schema: {
      autoRun: { type: 'boolean' },
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

  private _initToken(): Promise<{
    userName: string;
    token: string;
  }> {
    return new Promise((resolve, reject) => {
      (async () => {
        try {
          if (this.tokenDataReceiver) {
            this.tokenDataReceiver.reject();
          }
          this.tokenDataReceiver = {
            resolve: (tokenData) => resolve(tokenData),
            reject: reject,
          };
          this._window.hide();
          await this._window.loadURL(options.autologin);
        } catch (e) {
          this.tokenDataReceiver?.reject();
          this.tokenDataReceiver = undefined;
          const code = e?.['code'];
          if (['ERR_CONNECTION_REFUSED', 'ERR_FAILED'].includes(code)) {
            this._onDisconnected();
          } else {
            this._refreshTrayIcon();
            await this._loadHash(this._window, `warn/${code ?? 'unavailable'}`);
            this._window.show();
          }
          reject(e);
        }
      })();
    });
  }

  private async _loadHash(window: BrowserWindow | WebContents, hash: string) {
    if (!app.isPackaged && process.env['ELECTRON_RENDERER_URL']) {
      await window.loadURL(`${process.env['ELECTRON_RENDERER_URL']}#${hash}`);
    } else {
      await window.loadFile(path.join(__dirname, `../renderer/index.html`), {hash: hash});
    }
  }

  private async _initMainWindow() {
    this._session = session.fromPartition('persist:cimon', { cache: true });
    this._session.allowNTLMCredentialsForDomains('*');
    await this._session.clearStorageData();
    await this._session.clearCache();
    await this._session.cookies.set({
      name: 'Cimon-ClientType',
      value: 'Electron',
      url: options.baseUrl,
    });
    this._window = new BrowserWindow({
      webPreferences: {
        preload: path.join(__dirname, '..', 'preload', 'cimon.cjs'),
        session: this._session,
        allowRunningInsecureContent: true,
      },
      show: false,
      paintWhenInitiallyHidden: false,
      autoHideMenuBar: !isDev,
    });
    this._window.on('close', (evt) => {
      if (this._isExiting) return;
      evt.preventDefault();
      this._window?.hide();
    });
    this._window.on('minimize', (evt) => {
      evt.preventDefault();
      this._window?.hide();
    });
    this._window.on('show', () => {
      this._updateContextMenuVisibility();
    });
    this._window.on('hide', () => {
      this._updateContextMenuVisibility();
    });
    this._hideWindowOnEsc(this._window);
    this._window.webContents.on('will-navigate', async (e, url) => {
      if (this._getIsDiscussionUrl(url)) {
        e.preventDefault();
        await this._openBuildDiscussionInWindow(url);
        return;
      }
      if (url.startsWith(options.baseUrl)) {
        return;
      }
      e.preventDefault();
      await shell.openExternal(url);
    });
    this._window.webContents.setWindowOpenHandler(({ url }) => {
      if (this._getIsDiscussionUrl(url)) {
        this._openBuildDiscussionInWindow(url);
        return { action: 'deny' };
      }
      if (url.startsWith(options.baseUrl)) {
        return { action: 'allow' };
      }
      shell.openExternal(url);
      return { action: 'deny' };
    });
    this._window.webContents.on('did-navigate-in-page', (_, url) => this._onRedirectedToLogin(url));
    this._window.webContents.on('did-navigate', (_, url) => this._onRedirectedToLogin(url));
  }

  private _loginPageUrl = '/Login';

  private async _onRedirectedToLogin(url: string) {
    if (url.endsWith(this._loginPageUrl)) {
      let windowWasHidden = false;
      if (this._window.isVisible()) {
        this._window.hide();
        windowWasHidden = true;
      }
      await this._openLoginWindow();
      this._loginWindow!.webContents.once('did-redirect-navigation', async () => {
        this._loginWindow?.destroy();
        this._loginWindow = undefined;
        await this._window.loadURL(options.entrypoint);
        if (windowWasHidden) {
          this._window.show();
        }
      });
    }
  }

  private _getIsDiscussionUrl(url: string) {
    return url.startsWith(options.discussionWindowUrl);
  }

  private async _openBuildDiscussionInWindow(url: string) {
    const path = url.split('/');
    const buildId = path[path.length - 1];
    await this._onOpenDiscussionWindow(`/buildDiscussion/${buildId}`);
  }

  private _onDisconnected() {
    this._window?.hide();
    this._tray.setToolTip(`Waiting for connection to: ${options.baseUrl}. `);
    this._refreshTrayIcon();
  }

  private _trayContextMenuVisibilityConfigs: Array<{
    id: string;
    fn: () => boolean;
  }> = [];

  private _updateContextMenuVisibility() {
    for (const item of this._trayContextMenuVisibilityConfigs) {
      const menu = this._trayContextMenu.items.find((x) => x.id === item.id);
      if (menu) {
        menu.visible = item.fn();
      }
    }
  }

  private _onConnected() {
    this._refreshTrayIcon();
    this._tray.setToolTip(`cimon - continuous integration monitoring [${options.baseUrl}]`);
  }

  private async _openLoginWindow() {
    if (this._loginWindow == null || this._loginWindow.isDestroyed()) {
      this._loginWindow = new BrowserWindow({
        webPreferences: {
          session: this._session,
          allowRunningInsecureContent: true,
        },
        show: false,
        paintWhenInitiallyHidden: true,
        autoHideMenuBar: true,
        center: true,
        width: 320,
        height: 360,
        minimizable: false,
        maximizable: false,
      });
      this._hideWindowOnEsc(this._loginWindow);
    }
    await this._loginWindow.loadURL(options.baseUrl + this._loginPageUrl);
    this._loginWindow.center();
    this._loginWindow.show();
  }

  private async _onOpenDiscussionWindow(url: string) {
    if (!this._discussionWindow || this._discussionWindow.isDestroyed()) {
      this._discussionWindow = new BrowserWindow({
        webPreferences: {
          session: this._session,
          allowRunningInsecureContent: true,
        },
        show: false,
        paintWhenInitiallyHidden: true,
        autoHideMenuBar: true,
        center: true,
        width: 600,
        height: 800,
        modal: true,
        parent: this._window,
      });
      this._hideWindowOnEsc(this._discussionWindow, 'close');
      this._discussionWindow.on('closed', () => {
        delete this._discussionWindow;
      });
      this._discussionWindow.webContents.on('will-navigate', async (e, url) => {
        e.preventDefault();
        if (this._getIsDiscussionUrl(url)) {
          await this._discussionWindow?.loadURL(url);
          return;
        }
        await shell.openExternal(url);
      });
      this._discussionWindow.webContents.setWindowOpenHandler(({ url }) => {
        if (this._getIsDiscussionUrl(url)) {
          this._discussionWindow?.loadURL(url);
          return { action: 'deny' };
        }
        shell.openExternal(url);
        return { action: 'deny' };
      });
    }
    await this._discussionWindow.loadURL(options.discussionUrl(url));
    this._discussionWindow.show();
  }

  private _currentState?: ConnectionState;

  private async _onConnectionStateChanged(state: ConnectionState) {
    if (this._currentState == state) {
      return;
    }
    this._monitorInfo = undefined;
    log.info(`SignalR state ${state}`);
    const previousState = this._currentState;
    this._currentState = state;
    this._updateContextMenuVisibility();
    if (state === ConnectionState.Connected) {
      await NotifierWrapper.hide('connection');
      this._onConnected();
      if (previousState == ConnectionState.Disconnected || previousState == ConnectionState.FailedToConnect) {
        await NotifierWrapper.notify('connection', {
          title: 'All good',
          subtitle: `Connected`,
        });
      }
      await new Promise<void>((r) => setTimeout(r, 5000));
      await NotifierWrapper.hide('connection');
      return;
    }
    if (ConnectionState.Disconnected === state) {
      this._onDisconnected();
      await NotifierWrapper.notify('connection', {
        title: 'Something went wrong',
        subtitle: `Connection lost`,
      });
      return;
    }
    if (ConnectionState.FailedToConnect === state && previousState !== ConnectionState.Disconnected) {
      this._onDisconnected();
      await NotifierWrapper.notify('connection', {
        title: 'Oops',
        subtitle: "Can't connect",
      });
      return;
    }
  }

  public async init() {
    this._autoLaunch = new AutoLaunch({
      name: 'cimon',
    });
    this._autoLaunch.opts.appName = 'cimon';
    await this._initSettings();
    this._subscribeForEvents();
    await this._initTray();
    await this._initSignalR();
  }

  private async _initSignalR() {
    this._signalR = new SignalRClient(options.baseUrl, (error) => this._getToken(error));
    this._signalR.onConnectionStateChanged = this._onConnectionStateChanged.bind(this);
    this._signalR.onOpenDiscussionWindow = this._onOpenDiscussionWindow.bind(this);
    this._signalR.onMentionsChanged = this._onMentionsChanged.bind(this);
    this._signalR.onMonitorInfoChanged = this._onMonitorInfoChanged.bind(this);
    await this._startSignalR();
  }

  private async _initTray() {
    this._tray = new Tray(options.icons.red.tray);
    this._rebuildMenu();
    this._trayContextMenuVisibilityConfigs.push({
      id: 'reconnect',
      fn: () => this._currentState !== ConnectionState.Connected && !this._waitForConnectionTimeout,
    });
    this._trayContextMenuVisibilityConfigs.push({
      id: 'showMonitor',
      fn: () => this._currentState === ConnectionState.Connected && !this._window.isVisible(),
    });
    this._trayContextMenuVisibilityConfigs.push({
      id: 'reload',
      fn: () => this._currentState === ConnectionState.Connected && this._window.isVisible(),
    });
    this._tray.setToolTip('cimon - connecting...');
    this._tray.on('click', async () => this.showMonitors());
    await this._initMainWindow();
  }

  private _restartMenuClicked = false;

  private _rebuildMenu() {
    const versionMenuLabel = this._updateReady
      ? `Restart to update`
      : `Version: ${app.getVersion()}. User ${this._userName}`;
    const template: MenuItemConstructorOptions[] = [
      {
        id: 'showMonitor',
        label: 'Show',
        type: 'normal',
        visible: false,
        click: async () => await this.showMonitors(),
      },
      {
        id: 'reload',
        label: 'Reload',
        type: 'normal',
        role: 'reload',
      },
      {
        id: 'reconnect',
        label: 'Reconnect',
        type: 'normal',
        click: async () => await this.reconnect(),
        visible: false,
      },
      {
        id: 'version',
        label: versionMenuLabel,
        enabled: this._updateReady,
        click: async () => {
          if (this._restartMenuClicked) {
            app.exit();
            return;
          }
          this._restartMenuClicked = true;
          await this._signalR.disconnect();
          this.quitAndUpdate();
        },
      },
      { id: 'options', label: 'Options', type: 'normal', click: () => this.showOptions() },
      { id: 'exit', label: 'Exit', type: 'normal', click: () => this.quit() },
    ];
    if (this._mentions.length > 0) {
      const submenu: MenuItemConstructorOptions[] = this._mentions.map((mi) => ({
        id: `mention_${mi.buildConfigId}`,
        label: `${mi.buildConfigKey} (${mi.commentsCount})`,
        click: () => this._onOpenDiscussionWindow(`/buildDiscussion/${mi.buildConfigId}`),
      }));
      template.splice(1, 0, {
        id: 'mentions',
        label: `Mentions (${this._mentions.length})`,
        submenu: submenu,
      });
    }
    this._trayContextMenu = Menu.buildFromTemplate(template);
    this._tray.setContextMenu(this._trayContextMenu);
    if (process.platform === 'darwin') {
      app.dock.setMenu(this._trayContextMenu);
    }
    this._updateContextMenuVisibility();
  }

  private async _onMentionsChanged(mentions: MentionInfo[]) {
    if (JSON.stringify(this._mentions) === JSON.stringify(mentions)) return;
    this._mentions = mentions ?? [];
    this._rebuildMenu();
  }

  private async _onMonitorInfoChanged(monitorInfo: MonitorInfo) {
    this._monitorInfo = monitorInfo;
    this._refreshTrayIcon();
    if (this._store.store.screenshots?.save) {
      await this._makeScreenshot(monitorInfo);
    }
  }

  private async _startSignalR() {
    await this._waitForConnection();
    try {
      await this._signalR.start();
    } catch (e) {
      log.error(e);
    }
  }

  private _waitForConnectionTimeout?: NodeJS.Timeout;

  private _connectionWaiter?: PromiseInfo<void>;

  private _waitForConnection() {
    if (this._currentState === ConnectionState.Connected) {
      return;
    }
    if (this._connectionWaiter) {
      this._connectionWaiter.reject();
    }
    return new Promise<void>((resolve, reject) => {
      this._connectionWaiter = {
        reject: reject,
        resolve: resolve,
      };
      // eslint-disable-next-line @typescript-eslint/no-unused-vars
      this._tryToConnect().then((_) => {
        /*empty*/
      });
    });
  }

  private async _tryToConnect() {
    try {
      const request = net.request(options.baseUrl);
      await new Promise<void>((res, rej) => {
        request.on('response', (response) => {
          if (response.statusCode === 200) {
            clearTimeout(this._waitForConnectionTimeout);
            this._connectionWaiter?.resolve();
            this._connectionWaiter = undefined;
            res();
          } else {
            rej(response.statusMessage);
          }
        });
        request.on('error', (error) => rej(error));
        request.end();
      });
    } catch (e) {
      console.warn('Failed to connect to baseUrl', e);
      this._waitForConnectionTimeout = setTimeout(() => this._tryToConnect(), options.waitForConnectionRetryDelay);
      this._onConnectionStateChanged(ConnectionState.FailedToConnect);
    }
  }

  private _subscribeForEvents() {
    ipcMain.handle('cimon-get-base-url', () => options.baseUrl);
    /*ipcMain.handle('cimon-show-window', async (event, code: 'login' | string) => { });*/
    ipcMain.handle('cimon-load', async (event, relativeUrl) => {
      await this._loadHash(event.sender, relativeUrl);
    });
    ipcMain.on('cimon-token-ready', (_: IpcMainEvent, tokenData: TokenInfo) => {
      this.tokenDataReceiver?.resolve(tokenData);
      this.tokenDataReceiver = undefined;
    });
    ipcMain.handle('dialog:selectDir', (_, defPath) => this.selectDirectory(defPath));
    ipcMain.handle('options:read', () => this._store.store);
    ipcMain.handle('options:save', (_, options) => this._updateOptions(options));
  }

  private _updateOptions(settings: NativeAppSettings) {
    try {
      this._store.set(settings);
      return {};
    } catch (e) {
      return {
        error: e,
      };
    }
  }

  private _subscribeForSettingsChanges() {
    this._store.onDidChange('autoRun', (newValue, oldValue) => {
      if (oldValue && !newValue) {
        this._autoLaunch.disable();
      } else if (newValue) {
        this._autoLaunch.enable();
      }
    });
  }

  private _token?: string = undefined;
  private _userName?: string;
  private _lastProvidedToken?: string = undefined;

  private async _getToken(error: (Error & { errorType?: string }) | undefined) {
    if (error?.errorType === 'FailedToNegotiateWithServerError') {
      this._token = undefined;
    }
    if (!this._token || this._lastProvidedToken === this._token) {
      await this._waitForConnection();
      const { token, userName } = await this._initToken();
      this._token = token;
      if (this._userName !== userName) {
        this._userName = userName;
        this._rebuildMenu();
      }
    }
    this._lastProvidedToken = this._token;
    return this._token;
  }

  public async reconnect() {
    await this._startSignalR();
  }

  public async showMonitors(): Promise<void> {
    await this._window.loadURL(options.lastMonitor);
    if (!this._window.webContents.getURL().endsWith(this._loginPageUrl)) {
      this._window.show();
    }
  }

  public setUpdateReady() {
    this._updateReady = true;
    this._rebuildMenu();
  }

  private _hideWindowOnEsc(window?: Electron.CrossProcessExports.BrowserWindow, action?: 'hide' | 'close') {
    if (!window || window.isDestroyed()) return;
    window.webContents.on('input-event', (event, input: Electron.InputEvent) => {
      if (input.type == 'rawKeyDown' && input['key'] === 'Escape') {
        if (window.isVisible()) {
          if (action === 'close') {
            window.close();
          } else {
            window.hide();
          }
          event.preventDefault();
        }
      }
    });
  }

  private _refreshTrayIcon() {
    const allOk = this._currentState === ConnectionState.Connected && !this._monitorInfo?.failedBuildsCount;
    const image = allOk ? options.icons.green.tray : options.icons.red.tray;
    this._tray.setImage(image);
  }

  private allowQuit() {
    this._isExiting = true;
  }

  public quit() {
    this.allowQuit();
    app.quit();
  }

  public quitAndUpdate() {
    this.allowQuit();
    autoUpdater.quitAndInstall();
  }

  private async showOptions() {
    if (this._optionsWindow && !this._optionsWindow.isDestroyed()) {
      this._optionsWindow.focus();
      return;
    }
    this._optionsWindow = new BrowserWindow({
      maximizable: false,
      minimizable: false,
      title: 'cimon - settings',
      width: 640,
      height: 450,
      center: true,
      autoHideMenuBar: !isDev,
      webPreferences: {
        preload: path.join(__dirname, '..', 'preload', 'internal.cjs'),
        webSecurity: false,
      },
    });
    this._optionsWindow.on('close', () => {
      this._optionsWindow = undefined;
    });
    await this._loadHash(this._optionsWindow, 'setup');
  }

  private async selectDirectory(defPath?: string) {
    const { canceled, filePaths } = await dialog.showOpenDialog({
      defaultPath: defPath,
      properties: ['openDirectory'],
    });
    if (!canceled) {
      return filePaths[0];
    }
    return undefined;
  }

  private async _initSettings() {
    const autorun = this._store.store.autoRun;
    const isAutorunEnabled = await this._autoLaunch.isEnabled();
    if (autorun !== isAutorunEnabled) {
      this._store.set('autoRun', isAutorunEnabled);
    }
    this._subscribeForSettingsChanges();
  }

  private async _makeScreenshot(monitorInfo: MonitorInfo) {
    const destPath = this._store.store.screenshots?.path;
    if (!destPath) {
      return;
    }
    const currentDate = new Date();
    const formattedDate = currentDate.toISOString().replace(/:/g, '-').replace(/\..+/, '');
    const filePath = path.join(
      destPath,
      `${monitorInfo.monitorKey}_${formattedDate}_failed_${monitorInfo.failedBuildsCount}.jpeg`
    );
    const width = this._store.store.screenshots?.width ?? 800;
    const height = this._store.store.screenshots?.height ?? 600;
    const quality = this._store.store.screenshots?.quality ?? 80;
    const win = new BrowserWindow({
      width: width,
      height: height,
      show: false,
      webPreferences: {
        offscreen: true,
        session: this._session,
      },
    });
    await win.loadURL(options.monitorUrl(monitorInfo.monitorKey));
    let firstFrame: number | undefined;
    win.webContents.on('paint', (_, __, image) => {
      if (!firstFrame) {
        firstFrame = Number(new Date());
        return;
      }
      if (Number(new Date()) - firstFrame > 5000) {
        fs.writeFileSync(filePath, image.toJPEG(quality));
        win.close();
      }
    });
    win.webContents.setFrameRate(1);
  }
}
