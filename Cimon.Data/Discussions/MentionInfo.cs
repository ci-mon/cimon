namespace Cimon.Data.Discussions;

public record MentionInfo(string BuildId, int CommentsCount)
{
	public int CommentsCount { get; set; } = CommentsCount;

}