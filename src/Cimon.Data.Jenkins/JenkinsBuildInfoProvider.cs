using System.Collections.Concurrent;
using System.Collections.Immutable;
using Cimon.Contracts.Services;
using Cimon.Jenkins;
using Cimon.Jenkins.Entities.Builds;
using Cimon.Jenkins.Entities.Users;
using JenkinsBuildInfo = Cimon.Jenkins.Entities.Builds.BuildInfo;

namespace Cimon.Data.Jenkins;

using System.Text;
using Cimon.Jenkins.Entities.Jobs;
using Cimon.Jenkins.WorkflowApi;
using Contracts.CI;

public class JenkinsBuildInfoProvider(ClientFactory factory) : IBuildInfoProvider
{
	record BuildInfoWrapper : BuildInfo
	{
		public required InternalBuildInfo InternalInfo { get; init; }
	}

	record InternalBuildInfo(JenkinsBuildInfo BuildInfo, JobLocator JobLocator);
	private static readonly ConcurrentDictionary<string, UserInfo?> UserNameMap = new();

	public async Task<IReadOnlyList<BuildInfo>> FindInfo(BuildInfoQuery infoQuery) {
		using var client = factory.Create(infoQuery.ConnectorInfo.ConnectorKey);
		var info = await GetFullBuildInfo(infoQuery, null, client);
		if (info is null) {
			return Array.Empty<BuildInfo>();
		}
		var results = new List<BuildInfo> {
			info
		};
		if (infoQuery.Options.IsInitialLoad || (info.Status == BuildStatus.Failed && info.Id != infoQuery.Options.LastBuildId)) {
			await LoadBuildHistory(infoQuery, info, client, results);
		}
		return results;
	}

	private async Task<BuildInfoWrapper?> GetFullBuildInfo(BuildInfoQuery infoQuery, long? number,
			IJenkinsClient client) {
		var build = await GetBuildInfo(infoQuery, number, client);
		if (build is null) return null;
		var buildInfo = build.BuildInfo;
		var name = buildInfo.FullDisplayName ?? buildInfo.DisplayName ?? infoQuery.BuildConfig.Key;
		if (buildInfo.FullDisplayName is not null && buildInfo.DisplayName is not null) {
			var index = buildInfo.FullDisplayName.LastIndexOf(buildInfo.DisplayName, StringComparison.Ordinal);
			name = buildInfo.FullDisplayName[..index];
		}
		var info = new BuildInfoWrapper {
			Name = name,
			Url = buildInfo.Url?.ToString()!,
			Group = null,
			BranchName = infoQuery.BuildConfig.Branch,
			Id = buildInfo.Id,
			StartDate = DateTimeOffset.FromUnixTimeMilliseconds(buildInfo.Timestamp),
			Duration = TimeSpan.FromMilliseconds(buildInfo.Duration),
			Status = GetStatus(buildInfo.Result),
			InternalInfo = build,
			Number = buildInfo.Number.ToString()
		};
		if (info.Status == BuildStatus.Failed) {
			await LoadProblems(client, info, build);
			await LoadTests(client, build, info);
			info.Changes = await GetChanges(buildInfo, client);
		}
		return info;
	}

	private static async Task LoadProblems(IJenkinsClient client, BuildInfoWrapper info, InternalBuildInfo build) {
		var workflow = await client.Query(new JenkinsWfApi.DescribeBuild(build.BuildInfo.Number.ToString(),
			build.JobLocator));
		if (workflow is null) return;
		var statusBuilder = new StringBuilder();
		var problems = new List<CIBuildProblem>();
		foreach (var stage in workflow.Stages) {
			const string? failedStatus = "FAILED";
			if (!stage.Status.Equals(failedStatus, StringComparison.OrdinalIgnoreCase)) {
				continue;
			}
			statusBuilder.AppendLine($"Stage: {stage.Name} {stage.Status}: {stage.Error?.Message}");
			var stageFull = await client.Query(stage);
			stageFull ??= stage;
			AddProblemsFromStage(stageFull, problems);
		}
		info.Problems = problems;
		info.StatusText = statusBuilder.ToString();
	}

	private static void AddProblemsFromStage(Stage stageFull, List<CIBuildProblem> problems) {
		if (stageFull.StageFlowNodes.Count == 0) {
			Error? error = stageFull.Error;
			if (error is { }) {
				var problem = new CIBuildProblem(CIBuildProblemType.StageExecutionError, error.Type, error.Message,
					null);
				problems.Add(problem);
			}
		}
		foreach (var flowNode in stageFull.StageFlowNodes) {
			Error? error = flowNode.Error;
			if (error is { }) {
				var problem = new CIBuildProblem(CIBuildProblemType.StageExecutionError,
					flowNode.Name, $"{stageFull.Name}. {error.Type}: {error.Message}",
					null);
				problems.Add(problem);
			}
		}
	}

	private async Task LoadBuildHistory(BuildInfoQuery infoQuery, BuildInfoWrapper lastBuildInfo, IJenkinsClient client,
		List<BuildInfo> results) {
		var lastBuildNumber = lastBuildInfo.InternalInfo.BuildInfo.Number;
		var prevBuild = lastBuildNumber - 1;
		var minNumber = Math.Max(0, lastBuildNumber - infoQuery.Options.Limit);
		while (prevBuild > minNumber) {
			var info = await GetFullBuildInfo(infoQuery, prevBuild, client);
			if (info is null) {
				break;
			}
			if (info.Id == infoQuery.Options.LastBuildId) {
				break;
			}
			results.Insert(0, info);
			if (!infoQuery.Options.IsInitialLoad && info.Status == BuildStatus.Success) {
				break;
			}
			prevBuild--;
		}
	}

	private static async Task LoadTests(IJenkinsClient client, InternalBuildInfo build, BuildInfoWrapper info) {
		if (build.BuildInfo.Actions?.Find(x => "hudson.tasks.junit.TestResultAction".Equals(x?.Class)) is {} testsResult) {
			if (testsResult.Props.Contains(new KeyValuePair<string, object?>("failCount", 0)) &&
				testsResult.Props.TryGetValue("totalCount", out var totalCount) && totalCount is > 0) {
				return;
			}
		}
		var tests = await client.Query(new JenkinsApi.TestsReport(
			new JenkinsApi.BuildInfo(build.BuildInfo.Id, build.JobLocator)));
		if (tests is not null) {
			info.FailedTests = tests.Suites
				.SelectMany(x => x.Cases.Select(c=>new{ @case = c,suite = x}))
				.Where(x=>x.@case.Status == "FAILED" || x.@case.Status == "REGRESSION")
				.Select(x => new CITestOccurence(x.@case.Name) {
					TestId = x.@case.Name,
					Details = string.Join(Environment.NewLine, x.suite.Name,
						x.@case.ErrorDetails,
						x.@case.ErrorStackTrace),
					Ignored = x.@case.Skipped,
					NewFailure = x.@case.FailedSince == build.BuildInfo.Number
				}).ToList();
			info.StatusText = $"{info.FailedTests.Count} test(s) failed";
		}
	}

	public async Task<string> GetLogs(LogsQuery logsQuery) {
		if (logsQuery.BuildInfo is not BuildInfoWrapper buildInfo) {
			return string.Empty;
		}
		using var client = factory.Create(logsQuery.ConnectorInfo.ConnectorKey);
		var internalBuildInfo = buildInfo.InternalInfo;
		var info = new JenkinsApi.BuildInfo(internalBuildInfo.BuildInfo.Id, internalBuildInfo.JobLocator);
		var query = new JenkinsApi.BuildConsole(info);
		return await client.Query(query) ?? string.Empty;
	}

	public BuildInfo GetNoDataPlaceholder(BuildInfoQuery query) => BuildInfo.NoData(query.BuildConfig);

	private async Task<InternalBuildInfo?> GetLastFinishedBuild(long number, JobLocator locator,
			IJenkinsClient client, string? lastBuildId) {
		while (number > 0) {
			var lastBuildNumber = number.ToString();
			if (lastBuildNumber == lastBuildId) {
				return null;
			}
			var buildInfo = await client.Query(new JenkinsApi.BuildInfo(lastBuildNumber, locator));
			if (buildInfo == null) {
				return null;
			}
			if (buildInfo.Building || buildInfo.InProgress) {
				number--;
				continue;
			}
			return new InternalBuildInfo(buildInfo, locator);
		}
		return null;
	}

	private async Task<InternalBuildInfo?> GetBuildInfo(BuildInfoQuery buildInfoQuery, long? number,
			IJenkinsClient client) {
		var buildConfig = buildInfoQuery.BuildConfig;
		var locator = buildConfig.Branch is { Length: > 0 }
			? JobLocator.Create(buildConfig.Key, buildConfig.Branch)
			: JobLocator.Create(buildConfig.Key);
		if (number is { } buildNumber) {
			return await GetLastFinishedBuild(buildNumber, locator, client, buildInfoQuery.Options.LastBuildId);
		}
		JobInfo? job = await client.Query(new JenkinsApi.Job(locator));
		if (job is null) return null;
		return await GetLastFinishedBuild(job.LastCompletedBuild.Number, locator, client,
			buildInfoQuery.Options.LastBuildId);
	}

	private async Task<IReadOnlyCollection<VcsChange>> GetChanges(JenkinsBuildInfo buildInfo,
			IJenkinsClient client) {
		if (!buildInfo.ChangeSets?.Any() ?? true) {
			return Array.Empty<VcsChange>();
		}
		var res = new List<VcsChange>();
		VcsUser? FindUserFromChangeSetItem(ChangeSetItem changeSetItem) {
			var email = changeSetItem.AuthorEmail;
			if (string.IsNullOrWhiteSpace(email)) return null;
			User author = changeSetItem.Author;
			if (author.FullName is null) return null;
			return new VcsUser(author.UserId, author.FullName, email);
		}
		if (buildInfo.ChangeSets is null) return res;
		foreach (var changeSetItem in buildInfo.ChangeSets.SelectMany(x=>x.Items)) {
			if (changeSetItem.Author.FullName is null) continue;
			var modifiedFiles = changeSetItem.Paths.Select(GetFileModification).ToImmutableArray();
			var author = FindUserFromChangeSetItem(changeSetItem);
			if (author is null) {
				var userInfo = await FindUserId(changeSetItem.Author.FullName, client);
				var email = userInfo?.Property
					.Find(x => x.Class == "hudson.tasks.Mailer$UserProperty")?.Props["address"]?.ToString();
				author = userInfo is null
					? new VcsUser("UnknownUser", changeSetItem.Author.FullName)
					: new VcsUser(userInfo.Id, changeSetItem.Author.FullName, email);
			}
			var change = new VcsChange(author, changeSetItem.Timestamp.ToDate(),
				changeSetItem.Message, modifiedFiles);
			res.Add(change);
		}
		return res;
	}

	private async Task<UserInfo?> FindUserId(string fullName, IJenkinsClient client) {
		if (UserNameMap.TryGetValue(fullName, out var userInfo)) {
			return userInfo;
		}
		var user = await client.Query(new JenkinsApi.UserInfo(fullName)).ConfigureAwait(false);
		userInfo = user;
		UserNameMap[fullName] = userInfo;
		return userInfo;
	}

	private FileModification GetFileModification(ChangeSetPath path) {
		var type = path.EditType switch {
			EditType.Add => FileModificationType.Add,
			EditType.Delete => FileModificationType.Delete,
			EditType.Edit => FileModificationType.Edit,
			_ => FileModificationType.Unknown
		};
		return new FileModification(type, path.File ?? "<unknown>");
	}

	private BuildStatus GetStatus(string? buildInfoResult) {
		return buildInfoResult switch {
			"FAILURE" => BuildStatus.Failed,
			"UNSTABLE" => BuildStatus.Failed,
			_ => BuildStatus.Success
		};
	}
}
