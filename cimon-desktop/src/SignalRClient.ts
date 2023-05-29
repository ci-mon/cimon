import notifier from "node-notifier";
import {publish} from "rxjs";
import {IRetryPolicy, RetryContext, HubConnectionBuilder, HubConnection} from "@microsoft/signalr";

export enum ConnectionState {
    Connected,
    Disconnected
}
class ReconnectionPolicy implements IRetryPolicy {
    nextRetryDelayInMilliseconds(retryContext: RetryContext): number | null {
        return 5000;
    }
}
export class SignalRClient{
    onConnectionStateChanged: (state: ConnectionState, errorMessage?: string) => void;
    private _userName: string;
    private _connection: HubConnection;
    constructor(private _baseUrl: string, private accessTokenFactory: () => Promise<string>) {
        this._connection = new HubConnectionBuilder()
            .withUrl(`${this._baseUrl}/hubs/user`, {
                accessTokenFactory: this.accessTokenFactory,
                withCredentials: true
            })
            .withAutomaticReconnect(new ReconnectionPolicy())
            .build();
        this._connection.onreconnecting(error => {
            this.onConnectionStateChanged(ConnectionState.Disconnected, error.message);
        });
        this._connection.onreconnected(() => {
            this.onConnectionStateChanged(ConnectionState.Connected);
        });
    }
    async start() {
         await this._connection.start();
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