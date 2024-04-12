import { NotificationConstructorOptions } from 'electron';
import {
  createNotifier,
  NotificationBody,
  NotificationSounds,
  registerAppId,
  StatusMessageType,
  unRegisterAppId,
  Notification,
  Notifier as WinToastNotifier,
} from 'node-win-toast-notifier';
import { build } from '../../../package.json';
import gravatar from 'nodejs-gravatar';
import { CimonNotifier } from './cimon-notifier';
import { MentionNotificationResult } from './mention-notification-result';
import { NotificationQuickReply } from './notification-quickReply';
import { MentionNotificationReaction } from './mention-notification-reaction';
import { NotificationInstance } from './notification-instance';
export class WindowsNotifier extends CimonNotifier {
  private _notifierControl!: WinToastNotifier;

  override async init(isDev: boolean, fixRegistration: boolean): Promise<void> {
    await super.init(isDev, fixRegistration);
    WinToastNotifier.ExecutableName = build.notifier_exe_name;
    if (isDev) {
      if (fixRegistration) {
        await unRegisterAppId(this.AppId);
      }
      await registerAppId(this.AppId);
    }
    this._notifierControl = await createNotifier({
      application_id: this.AppId, // use process.execPath after start menu fix
    });
  }

  public async createMentionNotification(title: string, comment: string, commentAuthorEmail?: string) {
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
    return await this._notifierControl.notify({
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
          content: 'Quick reply',
          arguments: 'sendQuickReply',
        },
      ],
    });
  }

  override async createNotification(config: NotificationConstructorOptions): Promise<NotificationInstance> {
    return await this._notifierControl.notify({
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
  }

  protected getMentionNotificationResult(notification: NotificationInstance): Promise<MentionNotificationResult> {
    const instance = notification as Notification;
    return new Promise((resolve) => {
      instance.onChange((result) => {
        switch (result.type) {
          case StatusMessageType.Activated: {
            const info = result.info;
            switch (info?.arguments) {
              case 'sendQuickReply': {
                const type = info.inputs['quickReply'];
                const map: Record<string, NotificationQuickReply> = {
                  wip: NotificationQuickReply.Wip,
                  rollback: NotificationQuickReply.RequestingRollback,
                  mute: NotificationQuickReply.RequestingMute,
                };
                resolve({ reaction: MentionNotificationReaction.QuickReply, quickReplyType: map[type] });
                break;
              }
              case 'sendReply':
                resolve({ reaction: MentionNotificationReaction.Reply, replyText: info.inputs['replyText'] });
                break;
              default:
                resolve({ reaction: MentionNotificationReaction.Activated });
            }
            break;
          }
          case StatusMessageType.Dismissed:
            resolve({ reaction: MentionNotificationReaction.Dismissed });
            break;
        }
      });
    });
  }
}
