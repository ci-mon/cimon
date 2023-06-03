import notifier from "node-notifier";
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  IRetryPolicy,
  RetryContext,
} from "@microsoft/signalr";

export enum ConnectionState {
  Connected,
  Disconnected,
}

class ReconnectionPolicy implements IRetryPolicy {
  nextRetryDelayInMilliseconds(retryContext: RetryContext): number | null {
    return 5000;
  }
}

export class SignalRClient {
  onConnectionStateChanged: (
    state: ConnectionState,
    errorMessage?: string
  ) => void;
  onOpenDiscussionWindow: (url: string) => void;
  private _userName: string;
  private _connection: HubConnection;

  constructor(
    private _baseUrl: string,
    private accessTokenFactory: () => Promise<string>
  ) {
    this._connection = new HubConnectionBuilder()
      .withUrl(`${this._baseUrl}/hubs/user`, {
        accessTokenFactory: this.accessTokenFactory,
        withCredentials: true,
      })
      .withAutomaticReconnect(new ReconnectionPolicy())
      .build();
    this._connection.onreconnecting((error) => {
      this.onConnectionStateChanged(
        ConnectionState.Disconnected,
        error.message
      );
    });
    this._connection.onreconnected(() => {
      this.onConnectionStateChanged(ConnectionState.Connected);
    });
    this._connection.on("NotifyWithUrl", this._onNotifyWithUrl.bind(this));
  }

  async start() {
   try {
     await this._connection.start();
     if (this._connection.state === HubConnectionState.Connected) {
       this.onConnectionStateChanged?.(ConnectionState.Connected);
     }
   } catch (e){
     this.onConnectionStateChanged?.(ConnectionState.Disconnected);
   }
  }

  private _onNotifyWithUrl(buildId: string, url: string, title: string, comment: string) {
    notifier.notify(
      {
        title: title,
        message: comment,
        actions: ["Open", "Dismiss", "WIP", "Rollback", "Mute"],
        dropdownLabel: "Action",
        wait: true
      },
      async (_: Error, result: string) => {
        if (result === "open") {
          this.onOpenDiscussionWindow?.(url);
          return;
        }
        if (result === "wip") {
          await this._replyToNotification(buildId, NotificationQuickReply.Wip);
        } else if (result === "Rollback".toLowerCase()) {
          await this._replyToNotification(buildId, NotificationQuickReply.RequestingRollback)
        } else if (result === "Mute".toLowerCase()) {
          await this._replyToNotification(buildId, NotificationQuickReply.RequestingMute)
        }
      }
    );
  }
  private async _replyToNotification(buildId: string, type: NotificationQuickReply){
    await this._connection.invoke('ReplyToNotification', buildId, type, null);
  }
}
export enum NotificationQuickReply {
  None = 0,
  Wip = 1,
  RequestingRollback = 2,
  RequestingMute = 3,
}
