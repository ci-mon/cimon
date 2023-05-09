import notifier from "node-notifier";

export class SignalRClient{
    constructor(private _baseUrl: string, private _userName: string, private _token: string) {
    }

    async start(mainWindow: Electron.CrossProcessExports.BrowserWindow) {
        // TODO
        notifier.notify({
            title: 'Hello',
            subtitle: 'cimon',
            message: `Token for ${this._userName} received!`,
            actions: ['Open', 'Skip'],
        }, () => {

            mainWindow.show();
        });
    }
}