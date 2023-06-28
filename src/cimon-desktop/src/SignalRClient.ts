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
  FailedToConnect,
}

class ReconnectionPolicy implements IRetryPolicy {
  private _lastError: Error;
  public get lastError(): Error {
    return this._lastError;
  }

  nextRetryDelayInMilliseconds(retryContext: RetryContext): number | null {
    this._lastError = retryContext.retryReason;
    return 5000;
  }
}

export class SignalRClient {
  onConnectionStateChanged: (
    state: ConnectionState,
    errorMessage?: string
  ) => void;
  onMentionsChanged: (
    mentions: []
  ) => void;
  onOpenDiscussionWindow: (url: string) => void;
  private _userName: string;
  private _connection: HubConnection;

  constructor(
    private _baseUrl: string,
    private accessTokenFactory: (error: Error) => Promise<string>
  ) {
    const reconnectPolicy = new ReconnectionPolicy();
    this._connection = new HubConnectionBuilder()
      .withUrl(`${this._baseUrl}/hubs/user`, {
        accessTokenFactory: () =>
          this.accessTokenFactory(reconnectPolicy.lastError),
        withCredentials: true,
      })
      .withAutomaticReconnect(reconnectPolicy)
      .build();
    this._connection.onreconnecting((error) => {
      this.onConnectionStateChanged(
        ConnectionState.Disconnected,
        error.message
      );
    });
    this._connection.onreconnected(async () => {
      this.onConnectionStateChanged(ConnectionState.Connected);
      await this._connection.invoke('SubscribeForMentions');
    });
    this._connection.on("NotifyWithUrl", this._onNotifyWithUrl.bind(this));
    this._connection.on("UpdateMentions", mentions => this.onMentionsChanged?.(mentions));
  }

  async start() {
    await this._connection.start();
    if (this._connection.state === HubConnectionState.Connected) {
      this.onConnectionStateChanged?.(ConnectionState.Connected);
      await this._connection.invoke('SubscribeForMentions');
    }
  }

  private _onNotifyWithUrl(
    buildId: string,
    url: string,
    title: string,
    comment: string
  ) {
    notifier.notify(
      {
        title: title,
        message: comment,
        actions: ["Open", "Dismiss", "WIP", "Rollback", "Mute"],
        dropdownLabel: "Action",
        wait: true,
      },
      async (_: Error, result: string) => {
        if (result === "open") {
          this.onOpenDiscussionWindow?.(url);
          return;
        }
        if (result === "wip") {
          await this._replyToNotification(buildId, NotificationQuickReply.Wip);
        } else if (result === "Rollback".toLowerCase()) {
          await this._replyToNotification(
            buildId,
            NotificationQuickReply.RequestingRollback
          );
        } else if (result === "Mute".toLowerCase()) {
          await this._replyToNotification(
            buildId,
            NotificationQuickReply.RequestingMute
          );
        }
      }
    );
  }

  private async _replyToNotification(
    buildId: string,
    type: NotificationQuickReply
  ) {
    await this._connection.invoke("ReplyToNotification", buildId, type, null);
  }
}

export enum NotificationQuickReply {
  None = 0,
  Wip = 1,
  RequestingRollback = 2,
  RequestingMute = 3,
}
