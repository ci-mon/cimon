export interface NotificationInstance {
  sourceBuildId?: number;
  sourceNotification?: Electron.Notification;
  remove(): Promise<void>;
  onChange: (cb: () => void) => void;
}
