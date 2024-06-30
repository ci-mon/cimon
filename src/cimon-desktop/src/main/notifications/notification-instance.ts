export interface NotificationInstance {
  sourceBuildId?: string;
  sourceNotification?: Electron.Notification;
  remove(): Promise<void>;
  onChange: (cb: () => void) => void;
}
