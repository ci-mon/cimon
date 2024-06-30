import { build } from '../../../package.json';
import process from 'process';
import { NotificationConstructorOptions } from 'electron';

import { NotificationInstance } from './notification-instance';
import log from 'electron-log';
import { MentionNotificationResult } from './mention-notification-result';

export abstract class CimonNotifier {
  public AppId: string = build.appId;
  public static Instance: CimonNotifier;

  public init(isDev: boolean, fixRegistration: boolean): Promise<void> {
    if (isDev) {
      this.AppId = process.execPath;
      log.info(`Notifier: AppId set to ${this.AppId} fixRegistration: ${fixRegistration}`);
    }
    return Promise.resolve(undefined);
  }

  protected savedNotifications: Record<string, NotificationInstance> = {};

  protected abstract createNotification(config: NotificationConstructorOptions): Promise<NotificationInstance>;

  protected abstract createMentionNotification(
    title: string,
    comment: string,
    commentAuthorEmail?: string
  ): Promise<NotificationInstance>;

  public async notify(id: string, config: NotificationConstructorOptions): Promise<void> {
    await this.hide(id);
    this.saveNotification(id, await this.createNotification(config));
  }

  protected abstract getMentionNotificationResult(
    notification: NotificationInstance
  ): Promise<MentionNotificationResult>;

  async hide(id: string) {
    await this.savedNotifications[id]?.remove();
    delete this.savedNotifications[id];
  }

  saveNotification(id: string, notification: NotificationInstance) {
    this.savedNotifications[id] = notification;
    notification.onChange(() => {
      delete this.savedNotifications[id];
    });
  }

  public async showMentionNotification(buildId: number, title: string, comment: string, commentAuthorEmail?: string) {
    const id = 'commentNotification';
    await this.hide(id);
    const notification = await this.createMentionNotification(title, comment, commentAuthorEmail);
    notification.sourceBuildId = buildId;
    this.saveNotification(id, notification);
    return await this.getMentionNotificationResult(notification);
  }

  async hideAll() {
    for (const id of Object.keys(this.savedNotifications)) {
      await this.hide(id);
    }
  }

  async remove(buildId: number) {
    const id = 'commentNotification';
    if (this.savedNotifications[id]?.sourceBuildId === buildId) {
      await this.hide(id);
    }
  }
}
