import notifier from "node-notifier";
import {publish} from "rxjs";

export enum ConnectionState {
    Connected,
    Disconnected
}

export class SignalRClient{
    onConnectionStateChanged: (state: ConnectionState) => void;
    private _userName: string;
    private _token: string;
    constructor(private _baseUrl: string) {
    }
    public setTokenData(userName: string, token: string){
        this._userName = userName;
        this._token = token;
    }

    async start() {
        // TODO
        notifier.notify({
            title: 'Hello',
            subtitle: 'cimon',
            message: `Token for ${this._userName} received!`,
            actions: ['Open', 'Skip'],
        }, () => {
            console.log('notified')
        });
        this.onConnectionStateChanged?.(ConnectionState.Connected);
    }
}