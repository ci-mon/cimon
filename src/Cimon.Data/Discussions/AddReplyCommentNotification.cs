using MediatR;

namespace Cimon.Data.Discussions;

public class AddReplyCommentNotification : INotification
{
	public required string BuildId { get; init; }
	public required string Comment { get; init; }
	public QuickReplyType QuickReplyType { get; set; }
	
}
