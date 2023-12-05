using System.Collections.Concurrent;
using System.Collections.Immutable;
using Cimon.Contracts.Services;
using Narochno.Jenkins.Entities.Builds;

namespace Cimon.Data.Jenkins;

using Contracts.CI;

public class JenkinsBuildInfoProvider : IBuildInfoProvider
{
	private static readonly ConcurrentDictionary<string, Task<string>> UserNameMap = new();
	private readonly ClientFactory _factory;

	public JenkinsBuildInfoProvider(ClientFactory factory) {
		_factory = factory;
	}
	
	public async Task<BuildInfo?> FindInfo(BuildInfoQuery infoQuery) {
		var build = await GetBuildInfo(infoQuery);
		if (build == null) {
			return null;
		}
		var buildInfo = build.BuildInfo;
		var buildConfig = infoQuery.BuildConfig;
		var changes = await GetChanges(buildInfo);
		var name = buildInfo.FullDisplayName.Substring(0,
			buildInfo.FullDisplayName.LastIndexOf(buildInfo.DisplayName, StringComparison.Ordinal));
		var info = new BuildInfo {
			Name = name,
			Url = buildInfo.Url.ToString(),
			Group = null,
			BranchName = buildConfig.Branch,
			Number = buildInfo.Number.ToString(),
			StartDate = DateTimeOffset.FromUnixTimeMilliseconds(buildInfo.Timestamp),
			Duration = TimeSpan.FromMilliseconds(buildInfo.Duration),
			Status = GetStatus(buildInfo.Result),
			Changes = changes
		};
		var lastBuildNumber = infoQuery.Options?.LastBuildNumber;
		if (info.Status ==  BuildStatus.Failed && !string.IsNullOrWhiteSpace(lastBuildNumber) &&
				!lastBuildNumber.Equals(info.Number, StringComparison.OrdinalIgnoreCase)) {
			using var client = _factory.Create();
			info.Log = await client.GetBuildConsole(build.JobName, buildInfo.Id, default);
		}
		return info;
	}

	public Task<string> GetLogs(LogsQuery logsQuery) {
		return Task.FromResult(String.Empty);
	}

	record InternalBuildInfo(Narochno.Jenkins.Entities.Builds.BuildInfo? BuildInfo, BuildInfoQuery Query, string? JobName);
	private async Task<InternalBuildInfo?> GetBuildInfo(BuildInfoQuery buildInfoQuery) {
		using var client = _factory.Create();
		var buildConfig = buildInfoQuery.BuildConfig;
		var job = await client.GetJob(buildConfig.Key, default);
		if (string.IsNullOrEmpty(buildConfig.Branch)) {
			var lastBuildNumber = job.LastBuild.Number.ToString();
			var buildInfo = await client.GetBuild(job.Name, lastBuildNumber, default);
			return new InternalBuildInfo(buildInfo, buildInfoQuery, job.Name);
		}
		var branchJobId = job.Jobs.FirstOrDefault(x => x.Name == buildConfig.Branch);
		if (branchJobId == null) {
			return null;
		}
		var branchJobFullName = $"{job.Name}/job/{branchJobId.Name}";
		var branchJob = await client.GetJob(branchJobFullName, default);
		var lastBranchBuildNumber = branchJob.LastBuild.Number.ToString();
		var branchBuildInfo = await client.GetBuild(branchJobFullName, lastBranchBuildNumber, default);
		return new InternalBuildInfo(branchBuildInfo, buildInfoQuery, branchJobFullName);
	}

	private async Task<IReadOnlyCollection<VcsChange>> GetChanges(Narochno.Jenkins.Entities.Builds.BuildInfo buildInfo) {
		if (!buildInfo.ChangeSet.Items.Any()) {
			return Array.Empty<VcsChange>();
		}
		var res = new List<VcsChange>();
		foreach (var changeSetItem in buildInfo.ChangeSet.Items) {
			var modifiedFiles = changeSetItem.Paths.Select(GetFileModification).ToImmutableArray();
			var userName = await FindUserId(changeSetItem.Author.FullName);
			// TODO get user email from jenkins
			var author = new VcsUser(userName, changeSetItem.Author.FullName);
			var change = new VcsChange(author, changeSetItem.Timestamp.ToDate(),
				changeSetItem.Message, modifiedFiles);
			res.Add(change);
		}
		return res;
	}

	private async Task<string> FindUserId(string fullName) {
		return await UserNameMap.GetOrAdd(fullName, GetUserIdByName, _factory).ConfigureAwait(false);
		static async Task<string> GetUserIdByName(string fullName, ClientFactory factory) {
			using var client = factory.Create();
			var user = await client.GetUser(fullName, default);
			return user.Id;
		}
	}

	private FileModification GetFileModification(ChangeSetPath path) {
		var type = path.EditType switch {
			EditType.Add => FileModificationType.Add,
			EditType.Delete => FileModificationType.Delete,
			EditType.Edit => FileModificationType.Edit,
			_ => FileModificationType.Unknown
		};
		return new FileModification(type, path.File);
	}

	private BuildStatus GetStatus(string buildInfoResult) {
		return buildInfoResult switch {
			"FAILURE" => BuildStatus.Failed,
			_ => BuildStatus.Success
		};
	}
}
