using Cimon.Data.Discussions;

namespace Cimon.Data.Users;
public record ExtendedMentionInfo(int BuildConfigId, int CommentsCount, string BuildConfigKey) : MentionInfo(BuildConfigId, CommentsCount);

public record MonitorInfo()
{
	public required string MonitorKey { get; set; }
	public int FailedBuildsCount { get; set; }
}

public interface IUserClientApi
{
	Task NotifyWithUrl(int buildConfigId, string url, string header, string message, string authorEmail);
	Task UpdateMentions(IEnumerable<ExtendedMentionInfo> mentions);
	Task CheckForUpdates();
	Task UpdateMonitorInfo(MonitorInfo monitorInfo);
}
