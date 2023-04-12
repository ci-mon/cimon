import {IRetryPolicy, RetryContext} from "@microsoft/signalr/src/IRetryPolicy";
import {app, BrowserWindow, session} from "electron";
import {HubConnectionBuilder} from "@microsoft/signalr"
import * as request from "request"

class ReconnectionPolicy implements IRetryPolicy {
    nextRetryDelayInMilliseconds(retryContext: RetryContext): number | null {
        return 5000;
    }

}

const createWindow = async () => {
    session.defaultSession.allowNTLMCredentialsForDomains('*')

    const win = new BrowserWindow({
        width: 400,
        height: 800,
        autoHideMenuBar: true
    });

    // TODO open login page, get access token and use it in signalR connection
    win.loadURL('http://localhost:5001/auth');
}

app.whenReady().then(() => {
    createWindow()
})