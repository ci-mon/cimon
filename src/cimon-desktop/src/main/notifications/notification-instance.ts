export interface NotificationInstance {
  sourceNotification?: Electron.Notification;
  remove(): Promise<void>;
  onChange: (cb: () => void) => void;
}
