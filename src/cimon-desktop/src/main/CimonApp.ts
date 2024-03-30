import {
  app,
  autoUpdater,
  BrowserWindow,
  dialog,
  ipcMain,
  Menu,
  MenuItemConstructorOptions,
  session,
  shell,
  Tray,
  WebContents,
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
import { CimonConfig } from '../../cimon-config';

interface MentionInfo {
  buildConfigId: number;
  commentsCount: number;
  buildConfigKey: string;
}

type TokenInfo = {
  userName: string;
  token: string;
};

export class CimonApp {
  private _window?: Electron.CrossProcessExports.BrowserWindow;
  private _loginWindow?: Electron.CrossProcessExports.BrowserWindow;
  private _discussionWindow?: Electron.CrossProcessExports.BrowserWindow;
  private _optionsWindow?: Electron.CrossProcessExports.BrowserWindow;

  private _loggedIn = false;
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

  private async _initToken(): Promise<TokenInfo> {
    try {
      return await this._getTokenFromServer();
    } catch (e) {
      const code = e?.['code'];
      if (['ERR_CONNECTION_REFUSED', 'ERR_FAILED'].includes(code)) {
        this._onDisconnected();
      } else {
        this._refreshTrayIcon();
      }
      throw e;
    }
  }

  private async _loadHash(window: BrowserWindow | WebContents, hash: string) {
    if (!app.isPackaged && process.env['ELECTRON_RENDERER_URL']) {
      await window.loadURL(`${process.env['ELECTRON_RENDERER_URL']}#${hash}`);
    } else {
      await window.loadFile(path.join(__dirname, `../renderer/index.html`), { hash: hash });
    }
  }

  private async _initMainWindow() {
    this._window = new BrowserWindow({
      webPreferences: {
        session: this._session,
        allowRunningInsecureContent: true,
      },
      show: false,
      paintWhenInitiallyHidden: false,
      autoHideMenuBar: !isDev,
    });
    this._window.on('close', () => {
      if (this._isExiting) return;
      delete this._window;
      this._rebuildMenu();
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

  private async _getTokenFromServer() {
    const result = await this._session.fetch(options.tokenUrl, {
      credentials: 'same-origin',
    });
    return await result.json();
  }

  private _onLogin: Array<() => void> = [];
  private async _doLogin() {
    if (this._loginWindow) {
      this._loginWindow.show();
      this._loginWindow.focus();
      return new Promise<void>((r) => this._onLogin.push(r));
    }
    await this._waitForConnection();
    try {
      const response = await this._session.fetch(options.autologin);
      if (!response.ok) {
        await this._loginManually();
      }
    } catch {
      await this._loginManually();
    }
    const callbacks = this._onLogin;
    this._onLogin = [];
    for (const callback of callbacks) {
      callback();
    }
    this._loggedIn = true;
  }

  private async _loginManually() {
    const loginWindow = new BrowserWindow({
      webPreferences: {
        session: this._session,
        allowRunningInsecureContent: true,
        preload: path.join(__dirname, '..', 'preload', 'cimon.cjs'),
      },
      show: false,
      paintWhenInitiallyHidden: true,
      autoHideMenuBar: true,
      center: true,
      width: 320,
      height: 370,
      minimizable: false,
      maximizable: false,
    });
    this._loginWindow = loginWindow;
    this._hideWindowOnEsc(loginWindow);
    loginWindow.on('close', (e) => {
      if (this._isExiting) return;
      e.preventDefault();
      loginWindow.hide();
    });
    await loginWindow.loadURL(options.loginPageUrl);
    loginWindow.center();
    loginWindow.show();
    loginWindow.webContents.on('did-navigate', async (_, url, status) => {
      if (status !== 200) {
        let pageUrl = options.loginPageUrl;
        if (url.includes('autologin')) {
          pageUrl += '?error=autologinFailed';
        }
        await loginWindow.loadURL(pageUrl);
      }
    });
    await new Promise<void>((resolve) => {
      loginWindow.webContents.on('did-redirect-navigation', async (_, url) => {
        const path = new URL(url).pathname;
        const authPaths = [/^\/login$/i, /^\/auth\//i];
        if (authPaths.find((p) => p.exec(path))) {
          return;
        }
        loginWindow.destroy();
        delete this._loginWindow;
        resolve();
      });
    });
  }

  private async _onRedirectedToLogin(url: string) {
    if (options.isLoginPage(url)) {
      let windowWasHidden = false;
      if (this._window?.isVisible()) {
        this._window.hide();
        windowWasHidden = true;
      }
      await this._doLogin();
      if (windowWasHidden) {
        this._window?.show();
      }
      await this._window?.loadURL(options.lastMonitor);
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

  private _currentState = ConnectionState.Disconnected;

  private _hadConnection = false;

  private async _onConnectionStateChanged(state: ConnectionState) {
    if (this._currentState == state) {
      return;
    }
    this._monitorInfo = undefined;
    log.info(`State ${state}`);
    const previousState = this._currentState;
    this._currentState = state;
    this._updateContextMenuVisibility();
    if (state === ConnectionState.Connected) {
      await NotifierWrapper.hide('connection');
      this._onConnected();
      if (
        this._hadConnection &&
        (previousState == ConnectionState.Disconnected || previousState == ConnectionState.FailedToConnect)
      ) {
        await NotifierWrapper.notify('connection', {
          title: 'All good',
          subtitle: `Connected`,
        });
      }
      this._hadConnection = true;
      setTimeout(() => NotifierWrapper.hide('connection'), 5000);
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
    }
  }

  public async init() {
    this._autoLaunch = new AutoLaunch({
      name: 'cimon',
    });
    this._autoLaunch.opts.appName = 'cimon';
    await this._initSettings();
    this._subscribeForEvents();
    await this._initSession();
    await this._initTray();
    await this._doLogin();
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
    this._tray = new Tray(options.icons.red.tray, CimonConfig.trayId);
    this._rebuildMenu();
    this._trayContextMenuVisibilityConfigs.push({
      id: 'reconnect',
      fn: () => this._currentState !== ConnectionState.Connected,
    });
    this._trayContextMenuVisibilityConfigs.push({
      id: 'showMonitor',
      fn: () => this._loggedIn && this._currentState === ConnectionState.Connected && !this._window?.isVisible(),
    });
    this._trayContextMenuVisibilityConfigs.push({
      id: 'login',
      fn: () => !this._loggedIn,
    });
    this._trayContextMenuVisibilityConfigs.push({
      id: 'reload',
      fn: () => this._currentState === ConnectionState.Connected && !!this._window?.isVisible(),
    });
    this._tray.setToolTip('cimon - connecting...');
    this._tray.on('click', async () => this.showMonitors());
  }

  private _restartMenuClicked = false;

  private _rebuildMenu() {
    const versionMenuLabel = this._updateReady
      ? `Restart to update`
      : `Version: ${app.getVersion()}. ${this._userName ?? 'not connected yet'}`;
    const template: MenuItemConstructorOptions[] = [
      {
        id: 'showMonitor',
        label: 'Show',
        type: 'normal',
        visible: false,
        click: async () => await this.showMonitors(),
      },
      {
        id: 'login',
        label: 'Login',
        type: 'normal',
        visible: false,
        click: async () => await this._doLogin(),
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

  private async _waitForConnection() {
    if (this._currentState === ConnectionState.Connected) {
      return;
    }
    // eslint-disable-next-line no-constant-condition
    while (true) {
      try {
        const response = await this._session.fetch(options.baseUrl);
        if (response.ok) {
          await this._onConnectionStateChanged(ConnectionState.Connected);
          break;
        } else {
          log.warn(`Failed to connect to ${options.baseUrl}`);
        }
      } catch (e) {
        log.warn(`Failed to connect to ${options.baseUrl}`, e);
      }
      await new Promise((r) => setTimeout(r, 1000));
    }
  }

  private _subscribeForEvents() {
    ipcMain.handle('cimon-get-base-url', () => options.baseUrl);
    ipcMain.handle('cimon-load', async (event, relativeUrl) => {
      await this._loadHash(event.sender, relativeUrl);
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

  private async _getToken(error: (Error & { errorType?: string }) | undefined) {
    if (error) {
      this._token = undefined;
    }
    if (!this._token) {
      await this._waitForConnection();
      const { token, userName } = await this._initToken();
      this._token = token;
      this._userName = userName;
      this._rebuildMenu();
    }
    return this._token;
  }

  public async reconnect() {
    await this._startSignalR();
  }

  public async showMonitors(): Promise<void> {
    if (!this._loggedIn) {
      return;
    }
    if (!this._window) {
      await this._initMainWindow();
    }
    await this._window!.loadURL(options.lastMonitor);
    this._window!.show();
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

  private async _initSession() {
    this._session = session.fromPartition('persist:cimon', { cache: true });
    this._session.allowNTLMCredentialsForDomains('*');
    await this._session.clearStorageData();
    await this._session.clearCache();
    await this._session.cookies.set({
      name: 'Cimon-ClientType',
      value: 'Electron',
      url: options.baseUrl,
    });
  }
}
