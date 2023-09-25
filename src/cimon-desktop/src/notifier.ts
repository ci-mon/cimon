import {Notification, NotificationAction, NotificationConstructorOptions} from "electron";
import {createNotifier, NotificationBody, NotificationSounds, StatusMessage} from "node-win-toast-notifier";
import {Notifier as ToastNotifier} from "node-win-toast-notifier/lib/notifier";
import {build} from './../package.json';

export class Notifier {
    private static _saved: Record<string, Notification> = {};

    static notify(id: string, config: NotificationConstructorOptions) {
        this._saved[id]?.close();
        const notification = new Notification({
            ...config
        });
        this._saved[id] = notification;
        notification.show();
        notification.on("close", (_) => {
            this._saved[id] = undefined;
        });
    }


    private static _notifier: ToastNotifier;

    private async _getNotifier() {
        if (!Notifier._notifier) {
            Notifier._notifier = await createNotifier({
                application_id: build.appId, // use process.execPath after start menu fix
                connectToExistingService: false,
                port: 0
            });
        }
        // TODO Notifier._notifier.onClosed += recreate;
        return Notifier._notifier;
    }

    public async showCommentMentionNotificationOnWindows(title: string, comment: string, commentAuthorEmail: string = null) {
        const notifier = await this._getNotifier();
        const body: NotificationBody = [];
        if (commentAuthorEmail) {
            const gravatar = require('nodejs-gravatar');
            let imageUrl = gravatar.imageUrl(commentAuthorEmail);
            body.push({
                type: 'image',
                'hint-crop': 'circle',
                src: imageUrl,
                placement: "appLogoOverride"
            });
        }
        body.push({
                content: title,
                type: 'text'
            },
            {
                content: comment,
                type: 'text'
            });
        const notification = await notifier.notify({
            audio: {
                src: NotificationSounds.SMS
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
                            content: 'WIP'
                        },
                        {
                            id: 'rollback',
                            content: 'Rollback'
                        },
                        {
                            id: 'mute',
                            content: 'Mute please'
                        }
                    ]
                },
                {
                    id: 'replyText',
                    actionType: 'input',
                    type: 'text'
                },
                {
                    actionType: 'action',
                    content: 'Reply',
                    arguments: 'sendReply',
                    "hint-inputId": 'replyText'
                },
                {
                    actionType: 'action',
                    content: 'Open',
                    arguments: 'open',
                },
                {
                    actionType: 'action',
                    content: 'Quick reply',
                    arguments: 'sendQuickReply'
                }
            ]
        });
        return new Promise<StatusMessage>(res => {
            notification.onChange(statusMessage => {
                res(statusMessage);
            });
        });
    }
}
