using MediatR;

namespace Cimon.Data.Discussions;

record DiscussionClosedNotification(IBuildDiscussionService Discussion): INotification;