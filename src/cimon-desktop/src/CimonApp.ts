import Path from "path";
import request from "electron-request";

import { app, BrowserWindow, ipcMain, Menu, session, Tray } from "electron";
import { ConnectionState, SignalRClient } from "./SignalRClient";
import notifier from "node-notifier";
import IpcMainEvent = Electron.IpcMainEvent;

declare const MAIN_WINDOW_WEBPACK_ENTRY: string;
declare const MAIN_WINDOW_PRELOAD_WEBPACK_ENTRY: string;

class IconLocator {
  constructor(private _basename: string) {}

  public get normal() {
    return `${options.resourcesPath}/icons/${this._basename}/icon.png`;
  }

  public get tray() {
    return `${options.resourcesPath}/icons/${this._basename}/icon.ico`;
  }
}

const options = {
  baseUrl: "http://localhost:5001",
  waitForConnectionRetryDelay: 10000,

  get entrypoint() {
    return `${options.baseUrl}`;
  },
  get lastMonitor() {
    return `${options.baseUrl}/api/users/openLastMonitor?full-screen=true`;
  },
  get isDev() {
    return true;
  },
  get resourcesPath() {
    if (this.isDev) {
      return Path.join(__dirname, "..", "..");
    }
    return process.resourcesPath;
  },
  icons: {
    green: new IconLocator("green"),
    red: new IconLocator("red"),
  },
};
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
  private _window: Electron.CrossProcessExports.BrowserWindow;
  private _discussionWindow: Electron.CrossProcessExports.BrowserWindow;

  private tokenDataReceiver: TokenDataReceiver;

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
          await this._window.loadURL(options.entrypoint);
        } catch (e) {
          this.tokenDataReceiver.reject();
          this.tokenDataReceiver = null;
          if (["ERR_CONNECTION_REFUSED", "ERR_FAILED"].includes(e.code)) {
            this._onDisconnected();
          } else {
            this._tray.setImage(options.icons.red.tray);
            await this._window.loadURL(
              `${MAIN_WINDOW_WEBPACK_ENTRY}#warn/${e.code ?? "unavailable"}`
            );
            this._window.show();
          }
          reject(e);
        }
      })();
    });
  }

  private async _initMainWindow() {
    //this._session = session.fromPartition("persist:cimon", { cache: true }); //session.defaultSession;
    this._session = session.defaultSession;
    //this._session.allowNTLMCredentialsForDomains('*');
    this._window = new BrowserWindow({
      webPreferences: {
        preload: MAIN_WINDOW_PRELOAD_WEBPACK_ENTRY,
        session: this._session,
        allowRunningInsecureContent: true,
      },
      show: false,
      paintWhenInitiallyHidden: false,
      //autoHideMenuBar: true
      //fullscreen: true
    });
    //await this._window.webContents.openDevTools();
    this._window.on("close", (evt) => {
      evt.preventDefault();
      this._window.hide();
    });
  }

  private _onDisconnected() {
    this._window.hide();
    this._tray.setImage(options.icons.red.tray);
    this._tray.setToolTip(`Waiting for connection to: ${options.baseUrl}. `);
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
    this._tray.setImage(options.icons.green.tray);
    this._tray.setToolTip(
      `cimon - continuous integration monitoring [${options.baseUrl}]`
    );
  }

  private _tray: Tray;
  private _trayContextMenu: Electron.Menu;
  private _session: Electron.Session;
  private _signalR: SignalRClient;

  private async _onOpenDiscussionWindow(url: string) {
    if (this._discussionWindow == null) {
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
    }
    this._discussionWindow.on("closed", () => {
      this._discussionWindow = null;
    });
    await this._discussionWindow.loadURL(options.baseUrl + url);
    this._discussionWindow.show();
  }

  private _currentState: ConnectionState;

  private _onConnectionStateChanged(state: ConnectionState) {
    if (this._currentState == state) {
      return;
    }
    const previousState = this._currentState;
    this._currentState = state;
    this._updateContextMenuVisibility();
    if (state === ConnectionState.Connected) {
      this._onConnected();
      return;
    }
    if (ConnectionState.Disconnected === state) {
      this._onDisconnected();
      notifier.notify({
        title: "Something went wrong",
        message: `Connection lost`,
      });
      return;
    }
    if (
      ConnectionState.FailedToConnect === state &&
      previousState !== ConnectionState.Disconnected
    ) {
      this._onDisconnected();
      notifier.notify({
        title: "Oops",
        message: "Can't connect",
      });
      return;
    }
  }

  public async init() {
    this._subscribeForEvents();
    this._tray = new Tray(options.icons.red.tray);
    this._trayContextMenu = Menu.buildFromTemplate([
      {
        id: "showMonitor",
        label: "Show",
        type: "normal",
        visible: false,
        click: async () => await this.showMonitors(),
      },
      {
        id: "reconnect",
        label: "Reconnect",
        type: "normal",
        click: async () => await this.reconnect(),
        visible: false,
      },
      { id: "exit", label: "Exit", type: "normal", click: () => app.exit() },
    ]);
    this._trayContextMenuVisibilityConfigs.push({
      id: "reconnect",
      fn: () =>
        this._currentState !== ConnectionState.Connected &&
        !this._waitForConnectionTimeout,
    });
    this._trayContextMenuVisibilityConfigs.push({
      id: "showMonitor",
      fn: () => this._currentState === ConnectionState.Connected,
    });
    this._tray.setToolTip("cimon - connecting...");
    this._tray.setContextMenu(this._trayContextMenu);
    this._tray.on("click", async () => this.showMonitors());
    await this._initMainWindow();
    this._signalR = new SignalRClient(options.baseUrl, (error) =>
      this._getToken(error)
    );
    this._signalR.onConnectionStateChanged =
      this._onConnectionStateChanged.bind(this);
    this._signalR.onOpenDiscussionWindow =
      this._onOpenDiscussionWindow.bind(this);
    await this._startSignalR();
  }

  private async _startSignalR() {
    await this._waitForConnection();
    await this._signalR.start();
  }

  private _waitForConnectionTimeout: NodeJS.Timeout;

  private _connectionWaiter: PromiseInfo<void>;

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
      this._tryToConnect().then(_ => {/*empty*/});
    });
  }

  private async _tryToConnect() {
    try {
      const response = await request(options.baseUrl);
      if (response.ok) {
        this._connectionWaiter.resolve();
        this._connectionWaiter = null;
        return;
      }
    } catch (e) {
      /* empty */
    }
    this._waitForConnectionTimeout = setTimeout(
      () => this._tryToConnect(),
      options.waitForConnectionRetryDelay
    );
    this._onConnectionStateChanged(ConnectionState.FailedToConnect);
  }

  private _subscribeForEvents() {
    ipcMain.handle("cimon-get-base-url", () => options.baseUrl);
    ipcMain.handle("cimon-show-window", (event, code: "login" | string) => {
      if (code == "login") {
        this._window.webContents.once("did-redirect-navigation", (event) => {
          this._window.setSize(800, 600);
          this._window.center();
        });
        this._window.setSize(500, 260);
        this._window.show();
        this._window.center();
        this._window.webContents.session.webRequest.onBeforeRedirect(() => {
          this._window.hide();
          this._window.webContents.session.webRequest.onBeforeRedirect(null);
        });
      }
    });
    ipcMain.handle("cimon-load", async (event, relativeUrl) => {
      await event.sender.loadURL(`${MAIN_WINDOW_WEBPACK_ENTRY}#${relativeUrl}`);
    });
    ipcMain.on(
      "cimon-token-ready",
      (event: IpcMainEvent, tokenData: TokenInfo) => {
        this.tokenDataReceiver?.resolve(tokenData);
        this.tokenDataReceiver = null;
      }
    );
  }

  private _token: string = null;
  private _userName: string = null;
  private _lastProvidedToken: string = null;

  private async _getToken(error: Error & {errorType?: string}) {
    if (error?.errorType === 'FailedToNegotiateWithServerError') {
      this._token = null;
    }
    if (!this._token || (this._lastProvidedToken === this._token)) {
      await this._waitForConnection();
      const { token, userName } = await this._initToken();
      this._token = token;
      this._userName = userName;
    }
    this._lastProvidedToken = this._token;
    return this._token;
  }

  public async reconnect() {
    await this._startSignalR();
  }

  public async showMonitors(): Promise<void> {
    await this._window.loadURL(options.lastMonitor);
    this._window.show();
  }
}
