import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  IRetryPolicy,
  RetryContext,
} from '@microsoft/signalr';
import log from 'electron-log';
import { CimonNotifier } from './notifications/cimon-notifier';
import { NotificationQuickReply } from './notifications/notification-quickReply';
import { MentionNotificationReaction } from './notifications/mention-notification-reaction';
import { AutoUpdater } from './auto-updater';

export enum ConnectionState {
  Connected = 'Connected',
  Disconnected = 'Disconnected',
  FailedToConnect = 'FailedToConnect',
}

class ReconnectionPolicy implements IRetryPolicy {
  private _lastError?: Error;
  public get lastError(): Error | undefined {
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

  onConnectionStateChanged?: (state: ConnectionState, errorMessage?: string) => void;
  onMentionsChanged?: (mentions: []) => void;
  onMonitorInfoChanged?: (monitorInfo: MonitorInfo) => void;
  onOpenDiscussionWindow?: (url: string) => void;
  private _connection: HubConnection;

  constructor(
    private _baseUrl: string,
    private accessTokenFactory: (error: Error | undefined) => Promise<string>,
    private _notifier: CimonNotifier
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
      this.onConnectionStateChanged?.(ConnectionState.Disconnected, error?.message);
    });
    this._connection.onreconnected(async () => {
      this.onConnectionStateChanged?.(ConnectionState.Connected);
      await this._connection.invoke('SubscribeForMentions');
      await this._connection.invoke('SubscribeForLastMonitor');
    });
    this._connection.on(
      'NotifyWithUrl',
      (buildId: number, url: string, title: string, comment: string, authorEmail: string) => {
        this._onNotifyWithUrl(buildId, url, title, comment, authorEmail);
      }
    );
    this._connection.on('UpdateMentions', (mentions) => this.onMentionsChanged?.(mentions));
    this._connection.on('UpdateMonitorInfo', (monitorInfo) => this.onMonitorInfoChanged?.(monitorInfo));
    this._connection.on('CheckForUpdates', () => {
      log.info(`CheckForUpdates message received`);
      AutoUpdater.checkForUpdates();
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
    const result = await this._notifier.showMentionNotification(title, comment, authorEmail);
    switch (result.reaction) {
      case MentionNotificationReaction.Activated: {
        this.onOpenDiscussionWindow?.(url);
        break;
      }
      case MentionNotificationReaction.QuickReply: {
        await this._replyToNotification(buildId, result.quickReplyType!);
        break;
      }
      case MentionNotificationReaction.Reply: {
        await this._replyToNotification(buildId, NotificationQuickReply.None, result.replyText);
        break;
      }
    }
  }

  private async _replyToNotification(
    buildId: number,
    type: NotificationQuickReply,
    customReply: string | undefined = undefined
  ) {
    await this._connection.invoke('ReplyToNotification', buildId, type, customReply);
  }
}
