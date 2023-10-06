import {Notification, NotificationConstructorOptions} from "electron";
import {createNotifier, NotificationBody, NotificationSounds, StatusMessage} from "node-win-toast-notifier";
import {Notifier as ToastNotifier} from "node-win-toast-notifier/lib/notifier";
import {build} from './../package.json';
import process from "process";

interface INotification{
    close(): void;
}

export class NotifierWrapper {
    public static AppId: string = build.appId;
    private static _saved: Record<string, INotification> = {};

    static async notify(id: string, config: NotificationConstructorOptions) {
        this._saved[id]?.close();
        if (process.platform === 'win32'){
            const notifier = await NotifierWrapper._getNotifier();
            const notification = await notifier.notify({
                body: [
                    {
                        type: 'text',
                        content: config.title
                    },
                    {
                        type: 'text',
                        content: config.subtitle
                    }
                ],
            });
            notification.onChange(() => {
                delete this._saved[id];
            });
            this._saved[id] = {
                close() {
                    notification.remove();
                }
            };
            return;
        }
        const notification = new Notification({
            ...config
        });
        this._saved[id] = notification;
        notification.show();
        notification.on("close", (_) => {
            delete this._saved[id];
        });
    }


    private static _notifier: ToastNotifier;

    private static async _getNotifier() {
        if (!NotifierWrapper._notifier) {
            NotifierWrapper._notifier = await createNotifier({
                application_id: NotifierWrapper.AppId, // use process.execPath after start menu fix
            });
        }
        // TODO Notifier._notifier.onClosed += recreate;
        return NotifierWrapper._notifier;
    }

    public async showCommentMentionNotificationOnWindows(title: string, comment: string, commentAuthorEmail: string = null) {
        const notifier = await NotifierWrapper._getNotifier();
        const body: NotificationBody = [];
        if (commentAuthorEmail) {
            const gravatar = require('nodejs-gravatar');
            let imageUrl = gravatar.imageUrl(commentAuthorEmail, null).replace('http:', 'https:');
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
            settings: {image_cache: {enable: true}},
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
