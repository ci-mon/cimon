using Cimon.Contracts;
using MediatR;

namespace Cimon.Data.Discussions;

record DiscussionOpenNotification(IBuildDiscussionService Discussion, BuildInfo BuildInfo) : INotification;