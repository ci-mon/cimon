import { MentionNotificationReaction } from './mention-notification-reaction';
import { NotificationQuickReply } from './notification-quickReply';

export interface MentionNotificationResult {
reaction: MentionNotificationReaction;
replyText?: string;
quickReplyType?: NotificationQuickReply;
}
