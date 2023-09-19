import {Notification, NotificationConstructorOptions} from "electron";
import log from "electron-log";
import {createNotifier} from "node-win-toast-notifier";


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


    static async showWithActions(title: string, comment: string, actions: Electron.CrossProcessExports.NotificationAction[],
                           onAction: (selectedAction?: Electron.CrossProcessExports.NotificationAction) => Promise<void>) {
        const notifier = await createNotifier({
            application_id: 'notifier-test', // use process.execPath after start menu fix
            connectToExistingService: false,
            port: 7070,
        });
        const notification = await notifier.notify({
            body: 'Hello'
        });
        /*const toast = `\
<toast>
    <visual>
        <binding template="ToastGeneric">
            <text>${title}</text>
            <text>${comment}</text>
        </binding>
    </visual>
    <audio src='ms-winsoundevent:Notification.SMS' loop='false'/>
    <actions>
       
        <input id="replyType" type="selection" defaultInput="wip">
            <selection id="wip" content="WIP"/>
            <selection id="rollback" content="Rollback"/>
            <selection id="mute" content="Mute"/>
        </input>

        <action
                activationType="protocol"
                arguments="cimon-desktop://open"
                content="Open"/>
        <action
                activationType="protocol"
                arguments="cimon-desktop://reply"
                content="Reply"/>

        <action
                activationType="system"
                arguments="dismiss"
                content="Dismiss"/>

    </actions>
</toast>`;
        const notification = new Notification({
            title: title,
            subtitle: comment,
            toastXml: toast,
            actions: actions
        });
        notification.on('action', async (event, index) => {
            const selected = actions[index];
            await onAction(selected);
            notification.close();
        });
        notification.on('reply', event => {
            log.info('reply');
        });
        notification.on('click', event => {
            log.info('click');
        });
        notification.show();*/

    }
}
