import { Notification, NotificationConstructorOptions } from 'electron';
import { createNotifier, NotificationBody, NotificationSounds, StatusMessage } from 'node-win-toast-notifier';
import { Notifier as ToastNotifier } from 'node-win-toast-notifier/lib/notifier';
import { build } from './../../package.json';
import process from 'process';

import gravatar from 'nodejs-gravatar';

interface INotification {
  remove(): Promise<void>;
}

const savedNotifications: Record<string, INotification> = {};
export class NotifierWrapper {
  public static AppId: string = build.appId;

  static async hide(id: string) {
    await savedNotifications[id]?.remove();
    delete savedNotifications[id];
  }

  static async notify(id: string, config: NotificationConstructorOptions) {
    await NotifierWrapper.hide(id);
    if (process.platform === 'win32') {
      const notifier = await NotifierWrapper._getNotifier();
      const notification = await notifier.notify({
        body: [
          {
            type: 'text',
            content: config.title!,
          },
          {
            type: 'text',
            content: config.subtitle!,
          },
        ],
      });
      notification.onChange((statusMessage) => {
        if (statusMessage.dismissReason === 'UserCanceled' || statusMessage.type === 'Activated') {
          delete savedNotifications[id];
        }
      });
      savedNotifications[id] = notification;
      return;
    }
    const notification = new Notification({
      ...config,
    });
    savedNotifications[id] = {
      remove(): Promise<void> {
        notification.close();
        return Promise.resolve();
      },
    };
    notification.show();
    notification.on('close', () => {
      delete savedNotifications[id];
    });
  }

  private static _notifier: ToastNotifier;

  private static async _getNotifier() {
    if (!NotifierWrapper._notifier) {
      NotifierWrapper._notifier = await createNotifier({
        application_id: NotifierWrapper.AppId, // use process.execPath after start menu fix
      });
    }
    return NotifierWrapper._notifier;
  }

  public async showCommentMentionNotificationOnWindows(title: string, comment: string, commentAuthorEmail?: string) {
    const notifier = await NotifierWrapper._getNotifier();
    const body: NotificationBody = [];
    if (commentAuthorEmail) {
      const imageUrl = gravatar.imageUrl(commentAuthorEmail, null).replace('http:', 'https:');
      body.push({
        type: 'image',
        'hint-crop': 'circle',
        src: imageUrl,
        placement: 'appLogoOverride',
      });
    }
    body.push(
      {
        content: title,
        type: 'text',
      },
      {
        content: comment,
        type: 'text',
      }
    );
    const notification = await notifier.notify({
      settings: { image_cache: { enable: true } },
      audio: {
        src: NotificationSounds.SMS,
      },
      body: body,
      actions: [
        {
          id: 'quickReply',
          actionType: 'input',
          type: 'selection',
          defaultInput: 'mute',
          selection: [
            {
              id: 'wip',
              content: 'WIP',
            },
            {
              id: 'rollback',
              content: 'Rollback',
            },
            {
              id: 'mute',
              content: 'Mute please',
            },
          ],
        },
        {
          id: 'replyText',
          actionType: 'input',
          type: 'text',
        },
        {
          actionType: 'action',
          content: 'Reply',
          arguments: 'sendReply',
          'hint-inputId': 'replyText',
        },
        {
          actionType: 'action',
          content: 'Open',
          arguments: 'open',
        },
        {
          actionType: 'action',
          content: 'Quick reply',
          arguments: 'sendQuickReply',
        },
      ],
    });
    return new Promise<StatusMessage>((res) => {
      notification.onChange((statusMessage) => {
        res(statusMessage);
      });
    });
  }
}
