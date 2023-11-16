using MediatR;

namespace Cimon.Data.Discussions;

using Cimon.Contracts.CI;

record DiscussionOpenNotification(IBuildDiscussionService Discussion, BuildInfo BuildInfo) : INotification;