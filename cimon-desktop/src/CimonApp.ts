import Path from "path";
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
  get entrypoint() {
    return `${options.baseUrl}`;
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

export class CimonApp {
  private _window: Electron.CrossProcessExports.BrowserWindow;

  private _initToken(): Promise<{
    userName: string;
    token: string;
  }> {
    return new Promise( (resolve, reject) => {
      (async () => {
        try {
          ipcMain.once(
              "cimon-token-ready",
              async function (
                  event: IpcMainEvent,
                  tokenData: {
                    userName: string;
                    token: string;
                  }
              ) {
                resolve(tokenData)
              }.bind(this)
          );
          await this._window.loadURL(options.entrypoint);
        } catch (e) {
          if (e.code == "ERR_CONNECTION_REFUSED") {
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
    this._session = session.fromPartition("persist:cimon", { cache: true }); //session.defaultSession;
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
  }

  private _onDisconnected() {
    this._tray.setImage(options.icons.red.tray);
    this._tray.setToolTip(`Failed to connect to: ${options.baseUrl}`);
    this._trayContextMenu.items.find((i) => i.id === "reconnect").visible =
      true;
  }

  private _onConnected() {
    this._tray.setImage(options.icons.green.tray);
    this._tray.setToolTip(
      `cimon - continuous integration monitoring [${options.baseUrl}]`
    );
    this._trayContextMenu.items.find((i) => i.id === "reconnect").visible =
      false;
  }

  private _tray: Tray;
  private _trayContextMenu: Electron.Menu;
  private _session: Electron.Session;
  private _signalR: SignalRClient;

  private _onConnectionStateChanged(state: ConnectionState) {
    if (state === ConnectionState.Connected) {
      this._onConnected();
      return;
    }
    if (state === ConnectionState.Disconnected) {
      this._onDisconnected();
      notifier.notify({
        title: "Something went wrong",
        subtitle: "cimon",
        message: `Connection lost`,
      });
      return;
    }
  }

  public async init() {
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
      }
    });
    ipcMain.handle("cimon-load", async (event, relativeUrl) => {
      await event.sender.loadURL(`${MAIN_WINDOW_WEBPACK_ENTRY}#${relativeUrl}`);
    });

    this._tray = new Tray(options.icons.green.tray);
    this._trayContextMenu = Menu.buildFromTemplate([
      {
        id: "showMonitor",
        label: "Show",
        type: "normal",
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
    this._tray.setToolTip("cimon - connecting...");
    this._tray.setContextMenu(this._trayContextMenu);
    this._tray.on("click", async () => this.showMonitors());
    await this._initMainWindow();
    this._signalR = new SignalRClient(options.baseUrl, () => this._getToken());
    this._signalR.onConnectionStateChanged = (state) =>
      this._onConnectionStateChanged(state);
    await this._signalR.start();
  }

  private _token: string = null;
  private _lastProvidedToken: string = null;

  private async _getToken() {
    if (!this._token || this._lastProvidedToken === this._token) {
      const {token} = await this._initToken();
      this._token = token;
    }
    this._lastProvidedToken = this._token;
    return this._token;
  }

  public async reconnect(){
    // todo
  }
  public async showMonitors(): Promise<void> {
    this._window.show();
    notifier.notify({
      title: "Hello",
      subtitle: "cimon",
      message: `Monitors showed`,
    });
    //this._window.show();
  }
}
