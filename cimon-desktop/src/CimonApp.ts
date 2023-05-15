import Path from "path";
import {app, BrowserWindow, ipcMain, Menu, session, Tray} from "electron";
import {ConnectionState, SignalRClient} from "./SignalRClient";
import notifier from "node-notifier";

declare const MAIN_WINDOW_WEBPACK_ENTRY: string;
declare const MAIN_WINDOW_PRELOAD_WEBPACK_ENTRY: string;

class IconLocator {
    constructor(private _basename: string) {
    }
    public get normal(){
        return `${options.resourcesPath}/icons/${this._basename}/icon.png`
    }
    public get tray(){
        return `${options.resourcesPath}/icons/${this._basename}/icon.ico`
    }
}
const options = {
    baseUrl: 'http://localhost:5001',
    get entrypoint() {
        return `${options.baseUrl}/Desktop`;
    },
    get isDev(){
        return true;
    },
    get resourcesPath() {
        if (this.isDev) {
            return Path.join(__dirname, "..", "..");
        }
        return process.resourcesPath;
    },
    icons: {
        green: new IconLocator('green'),
        red: new IconLocator('red'),
    }
}
export class CimonApp {
    private async _initToken(){
        const window = new BrowserWindow({
            width: 800,
            height: 600,
            webPreferences: {
                preload: MAIN_WINDOW_PRELOAD_WEBPACK_ENTRY,
                session: this._session,
                allowRunningInsecureContent: true
            },
            show: false,
            //autoHideMenuBar: true
            //fullscreen: true
        });
        //await window.webContents.openDevTools();
        try {
            await window.loadURL(options.entrypoint);
        } catch (e) {
            if (e.code == 'ERR_CONNECTION_REFUSED') {
                this._onDisconnected();
                window.close();
            } else {
                this._tray.setImage(options.icons.red.tray);
                await window.loadURL(`${MAIN_WINDOW_WEBPACK_ENTRY}#warn/${e.code ?? "unavailable"}`);
                window.show();
            }
        }
    }
    private _onDisconnected(){
        this._tray.setImage(options.icons.red.tray);
        this._tray.setToolTip(`Failed to connect to: ${options.baseUrl}`);
        this._trayContextMenu.items.find(i=>i.id === 'reconnect').visible = true;
    }
    private _onConnected(){
        this._tray.setImage(options.icons.green.tray);
        this._tray.setToolTip(`Connected to: ${options.baseUrl}`);
        this._trayContextMenu.items.find(i=>i.id === 'reconnect').visible = false;
    }
    private _tray: Tray;
    private _trayContextMenu: Electron.Menu;
    private _session: Electron.Session;
    private _signalR: SignalRClient;

    private _onConnectionStateChanged(state: ConnectionState){
        if (state === ConnectionState.Connected) {
            this._onConnected();
            return;
        }
        if (state === ConnectionState.Disconnected){
            this._onDisconnected();
            notifier.notify({
                title: 'Something went wrong',
                subtitle: 'cimon',
                message: `Connection lost`,
            });
            return;
        }
    }
    public async init(){

        ipcMain.handle('cimon-get-base-url', () => options.baseUrl);
        ipcMain.handle('cimon-load', async (event, relativeUrl) => {
            await event.sender.loadURL(`${MAIN_WINDOW_WEBPACK_ENTRY}#${relativeUrl}`);
        });

        ipcMain.on('cimon-token-ready', async (event, tokenData) => {
            event.sender.close({waitForBeforeUnload: false});
            const {userName, token} = tokenData;
            if (this._signalR) {
                this._signalR.setTokenData(userName, token);
                return;
            }
            this._signalR = new SignalRClient(options.baseUrl);
            this._signalR.setTokenData(userName, token);
            this._signalR.onConnectionStateChanged = (state) => this._onConnectionStateChanged(state);
            await this._signalR.start();
        });

        this._session = session.defaultSession;//.fromPartition('cimon', {cache: true});
        this._session.allowNTLMCredentialsForDomains('*');

        this._tray = new Tray(options.icons.green.tray);
        this._trayContextMenu = Menu.buildFromTemplate([
            { id: 'showMonitor', label: 'Show', type: "normal", click: async () => await this.showMonitors()},
            { id: 'reconnect', label: 'Reconnect', type: "normal", click: async () => await this._initToken(), visible: false},
            { id: 'exit', label: 'Exit', type: "normal", click: () => app.exit()}
        ])
        this._tray.setToolTip('cimon - continuous integration monitoring')
        this._tray.setContextMenu(this._trayContextMenu);
        this._tray.on('click', async ()=> this.showMonitors());
        await this._initToken();
    }
    public async showMonitors(): Promise<void> {
        notifier.notify({
            title: 'Hello',
            subtitle: 'cimon',
            message: `Monitors showed`,
        });
        /*const window = new BrowserWindow({
            height: 600,
            width: 200,
            webPreferences: {
                preload: MAIN_WINDOW_PRELOAD_WEBPACK_ENTRY,
                session: this._session
            }
        });
        await window.loadURL(options.entrypoint);
        window.show();*/
    }
}