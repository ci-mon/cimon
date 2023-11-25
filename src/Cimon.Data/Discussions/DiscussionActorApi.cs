using Akka.Actor;

namespace Cimon.Data.Discussions;

public static class DiscussionActorApi
{
    internal record AddCommentMsg(CommentData CommentData);
    internal record UpdateCommentMsg(BuildComment Comment);
    internal record RemoveCommentMsg(BuildComment Comment);
    internal record ExecuteActionMsg(Guid ActionId);
    internal record SubscribeForState;
    internal record UnsubscribeForState;
    internal record SubscribeForComments;
    internal record UnsubscribeForComments;

    public static void AddComment(this ActorsApi.DiscussionHandle handle, CommentData commentData) =>
        handle.Discussion.Tell(new AddCommentMsg(commentData));
    public static void ExecuteAction(this ActorsApi.DiscussionHandle handle, Guid id) =>
        handle.Discussion.Tell(new ExecuteActionMsg(id));
    public static void UpdateComment(this ActorsApi.DiscussionHandle handle, BuildComment buildComment) =>
        handle.Discussion.Tell(new UpdateCommentMsg(buildComment));
    public static void RemoveComment(this ActorsApi.DiscussionHandle handle, BuildComment buildComment) =>
        handle.Discussion.Tell(new RemoveCommentMsg(buildComment));
}