import { CimonNotifier } from './cimon-notifier';
import { MentionNotificationResult } from './mention-notification-result';
import { NotificationQuickReply } from './notification-quickReply';
import { MentionNotificationReaction } from './mention-notification-reaction';
import { Notification, NotificationConstructorOptions } from 'electron';

import { NotificationInstance } from './notification-instance';

export class NotifierElectron extends CimonNotifier {
  protected createNotification(config: NotificationConstructorOptions): Promise<NotificationInstance> {
    const notification = new Notification({
      ...config,
    });
    notification.show();
    return Promise.resolve({
      sourceNotification: notification,
      remove(): Promise<void> {
        notification.close();
        return Promise.resolve();
      },
      onChange: (cb) => {
        notification.on('close', () => cb());
        notification.on('click', () => cb());
        notification.on('reply', () => cb());
        notification.on('failed', () => cb());
      },
    });
  }

  protected createMentionNotification(title: string, comment: string): Promise<NotificationInstance> {
    return this.createNotification({
      title: title,
      body: comment,
      hasReply: true,
      actions: [
        {
          text: 'Wip',
          type: 'button',
        },
      ],
    });
  }

  protected getMentionNotificationResult(notification: NotificationInstance): Promise<MentionNotificationResult> {
    const instance = notification.sourceNotification!;
    return new Promise((resolve) => {
      instance.on('reply', async (_, reply) => {
        resolve({ reaction: MentionNotificationReaction.Reply, replyText: reply });
      });
      instance.on('action', async (_, action) => {
        if (action === 0) {
          resolve({ reaction: MentionNotificationReaction.Activated });
        } else {
          resolve({ reaction: MentionNotificationReaction.QuickReply, quickReplyType: NotificationQuickReply.Wip });
        }
      });
      instance.on('close', () => {
        resolve({ reaction: MentionNotificationReaction.Dismissed });
      });
      instance.on('click', () => {
        resolve({ reaction: MentionNotificationReaction.Activated });
      });
    });
  }
}
