using System.Collections.Concurrent;
using System.Collections.Immutable;
using Cimon.Contracts.Services;
using Narochno.Jenkins;
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
	
	public async Task<IReadOnlyCollection<BuildInfo>> FindInfo(BuildInfoQuery infoQuery) {
		using var client = _factory.Create(infoQuery.ConnectorInfo.ConnectorKey);
		var build = await GetBuildInfo(infoQuery, client);
		var buildInfo = build?.BuildInfo;
		var buildConfig = infoQuery.BuildConfig;
		if (build is null || buildInfo is null) {
			return Array.Empty<BuildInfo>();
		}
		var changes = await GetChanges(buildInfo, client);
		var name = buildInfo.FullDisplayName.Substring(0,
			buildInfo.FullDisplayName.LastIndexOf(buildInfo.DisplayName, StringComparison.Ordinal));
		var info = new BuildInfo {
			Name = name,
			Url = buildInfo.Url.ToString(),
			Group = null,
			BranchName = buildConfig.Branch,
			Id = buildInfo.Id,
			StartDate = DateTimeOffset.FromUnixTimeMilliseconds(buildInfo.Timestamp),
			Duration = TimeSpan.FromMilliseconds(buildInfo.Duration),
			Status = GetStatus(buildInfo.Result),
			Changes = changes
		};
		if (buildInfo.Result == "UNSTABLE") {
			// load tests http://localhost:8080/job/test1/job/master/15/testReport/api/json
		}
		var lastBuildNumber = infoQuery.Options?.LastBuildId;
		if (info.Status ==  BuildStatus.Failed && !string.IsNullOrWhiteSpace(lastBuildNumber) &&
				!lastBuildNumber.Equals(info.Id, StringComparison.OrdinalIgnoreCase)) {
			info.Log = await client.GetBuildConsole(build.JobName, buildInfo.Id, default);
		}
		return new[] { info };
	}

	public Task<string> GetLogs(LogsQuery logsQuery) {
		return Task.FromResult(String.Empty);
	}

	record InternalBuildInfo(Narochno.Jenkins.Entities.Builds.BuildInfo? BuildInfo, BuildInfoQuery Query, string? JobName);
	private async Task<InternalBuildInfo?> GetBuildInfo(BuildInfoQuery buildInfoQuery, JenkinsClient client) {
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

	private async Task<IReadOnlyCollection<VcsChange>> GetChanges(Narochno.Jenkins.Entities.Builds.BuildInfo buildInfo,
			JenkinsClient client) {
		// todo changeset renamed to changesets
		if (!buildInfo.ChangeSet?.Items.Any() ?? true) {
			return Array.Empty<VcsChange>();
		}
		var res = new List<VcsChange>();
		foreach (var changeSetItem in buildInfo.ChangeSet.Items) {
			var modifiedFiles = changeSetItem.Paths.Select(GetFileModification).ToImmutableArray();
			var userName = await FindUserId(changeSetItem.Author.FullName, client);
			// TODO get user email from jenkins
			var author = new VcsUser(userName, changeSetItem.Author.FullName);
			var change = new VcsChange(author, changeSetItem.Timestamp.ToDate(),
				changeSetItem.Message, modifiedFiles);
			res.Add(change);
		}
		return res;
	}

	private async Task<string> FindUserId(string fullName, JenkinsClient client) {
		return await UserNameMap.GetOrAdd(fullName, GetUserIdByName, _factory).ConfigureAwait(false);
		async Task<string> GetUserIdByName(string fullName, ClientFactory factory) {
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
			"UNSTABLE" => BuildStatus.Failed,
			_ => BuildStatus.Success
		};
	}
}
