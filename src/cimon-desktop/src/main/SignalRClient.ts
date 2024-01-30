import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  IRetryPolicy,
  RetryContext,
} from '@microsoft/signalr';
import { autoUpdater, Notification } from 'electron';
import log from 'electron-log';
import { NotifierWrapper } from './notifierWrapper';
import { StatusMessageType } from 'node-win-toast-notifier';

export enum ConnectionState {
  Connected = 'Connected',
  Disconnected = 'Disconnected',
  FailedToConnect = 'FailedToConnect',
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

export interface MonitorInfo {
  monitorKey: string;
  failedBuildsCount: number;
}

export class SignalRClient {
  disconnect() {
    return this._connection.stop();
  }

  onConnectionStateChanged: (state: ConnectionState, errorMessage?: string) => void;
  onMentionsChanged: (mentions: []) => void;
  onMonitorInfoChanged: (monitorInfo: MonitorInfo) => void;
  onOpenDiscussionWindow: (url: string) => void;
  private _connection: HubConnection;
  private _notifier = new NotifierWrapper();

  constructor(
    private _baseUrl: string,
    private accessTokenFactory: (error: Error) => Promise<string>
  ) {
    const reconnectPolicy = new ReconnectionPolicy();
    this._connection = new HubConnectionBuilder()
      .withUrl(`${this._baseUrl}/hubs/user`, {
        accessTokenFactory: () => this.accessTokenFactory(reconnectPolicy.lastError),
        withCredentials: true,
      })
      .withAutomaticReconnect(reconnectPolicy)
      .build();
    this._connection.onreconnecting((error) => {
      this.onConnectionStateChanged(ConnectionState.Disconnected, error.message);
    });
    this._connection.onreconnected(async () => {
      this.onConnectionStateChanged(ConnectionState.Connected);
      await this._connection.invoke('SubscribeForMentions');
      await this._connection.invoke('SubscribeForLastMonitor');
    });
    this._connection.on('NotifyWithUrl', this._onNotifyWithUrl.bind(this));
    this._connection.on('UpdateMentions', (mentions) => this.onMentionsChanged?.(mentions));
    this._connection.on('UpdateMonitorInfo', (monitorInfo) => this.onMonitorInfoChanged?.(monitorInfo));
    this._connection.on('CheckForUpdates', () => {
      log.info(`CheckForUpdates message received`);
      autoUpdater.checkForUpdates();
    });
  }

  async start() {
    await this._connection.start();
    if (this._connection.state === HubConnectionState.Connected) {
      this.onConnectionStateChanged?.(ConnectionState.Connected);
      await this._connection.invoke('SubscribeForMentions');
      await this._connection.invoke('SubscribeForLastMonitor');
    }
  }

  private async _onNotifyWithUrl(buildId: number, url: string, title: string, comment: string, authorEmail: string) {
    // TODO refactor this check to notification wrapper
    if (process.platform === 'darwin') {
      const notification = new Notification({
        title: title,
        body: comment,
        hasReply: true,
        actions: [
          {
            text: 'Open',
            type: 'button',
          },
          {
            text: 'Wip',
            type: 'button',
          },
        ],
      });
      notification.show();
      notification.on('reply', async (e, reply) => {
        await this._replyToNotification(buildId, NotificationQuickReply.None, reply);
      });
      notification.on('action', async (e, action) => {
        if (action === 0) {
          this.onOpenDiscussionWindow?.(url);
          return;
        } else {
          await this._replyToNotification(buildId, NotificationQuickReply.Wip);
        }
      });
      return;
    }
    let result = await this._notifier.showCommentMentionNotificationOnWindows(title, comment, authorEmail);
    switch (result.type) {
      case StatusMessageType.Activated: {
        switch (result.info.arguments) {
          case 'open':
            this.onOpenDiscussionWindow?.(url);
            break;
          case 'sendQuickReply':
            const type = result.info.inputs['quickReply'];
            const map: Record<string, NotificationQuickReply> = {
              wip: NotificationQuickReply.Wip,
              rollback: NotificationQuickReply.RequestingRollback,
              mute: NotificationQuickReply.RequestingMute,
            };
            await this._replyToNotification(buildId, map[type]);
            break;
          case 'sendReply':
            await this._replyToNotification(buildId, NotificationQuickReply.None, result.info.inputs['replyText']);
            break;
        }
      }
    }
  }

  private async _replyToNotification(buildId: number, type: NotificationQuickReply, customReply: string = null) {
    await this._connection.invoke('ReplyToNotification', buildId, type, customReply);
  }
}

export enum NotificationQuickReply {
  None = 0,
  Wip = 1,
  RequestingRollback = 2,
  RequestingMute = 3,
}
