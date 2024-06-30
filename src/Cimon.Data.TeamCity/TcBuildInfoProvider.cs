using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using Cimon.Contracts.Services;
using TeamCityAPI;
using TeamCityAPI.Locators;
using TeamCityAPI.Models;
using TeamCityAPI.Queries;
using TeamCityAPI.Queries.Common;

namespace Cimon.Data.TeamCity;

using Contracts.CI;
using Newtonsoft.Json;
using TeamCityAPI.Locators.Enums;

class AdvancedBuildLocator : BuildLocator
{
	[JsonProperty("sinceBuild", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
	public BuildLocator? SinceBuild { get; set; }
}

public class TcBuildInfoProvider : IBuildInfoProvider
{

	private readonly TcClientFactory _clientFactory;

	public TcBuildInfoProvider(TcClientFactory clientFactory) {
		_clientFactory = clientFactory;
	}

	private readonly string _dateFormat = "yyyyMMdd'T'HHmmsszzz";

	private DateTimeOffset? ParseDate(string teamcityDate) {
		if (string.IsNullOrWhiteSpace(teamcityDate)) {
			return null;
		}
		return DateTimeOffset.ParseExact(teamcityDate, _dateFormat, CultureInfo.InvariantCulture);
	}

	public async Task<IReadOnlyList<BuildInfo>> FindInfo(BuildInfoQuery infoQuery) {
		using var clientTicket = _clientFactory.Create(infoQuery.ConnectorInfo.ConnectorKey);
		var build = await GetBuild(infoQuery, clientTicket);
		if (build is null || infoQuery.Options.LastBuildId == build.Id.ToString())
			return Array.Empty<BuildInfo>();
		TcBuildInfo info = await GetBuildInfo(build, clientTicket);
		var results = new List<BuildInfo> { info };
		if (info.Status == BuildStatus.Failed || infoQuery.Options.IsInitialLoad) {
			await LoadBuildHistory(clientTicket, results, infoQuery, build);
		}
		return results;
	}

	private async Task LoadBuildHistory(TeamCityClientTicket clientTicket, List<BuildInfo> results,
			BuildInfoQuery query, Build sourceBuild) {
		var lastNumberStr = query.Options.LastBuildId;
		var prevBuildId = (int)sourceBuild.Id!;
		prevBuildId--;
		if (sourceBuild.Id.ToString() == lastNumberStr || $"{prevBuildId}".Equals(lastNumberStr)) {
			return;
		}
		var buildLocator = GetBuildLocator(query.BuildConfig);
		var limit = query.Options.Limit;
		buildLocator.Count = limit;
		buildLocator.LookupLimit = 20;
		var buildsList = clientTicket.Client.Builds.Include(x => x.Build).WithLocator(buildLocator)
			.GetAsyncEnumerable<Builds, Build>(5);
		var count = 0;
		await foreach (var buildInfoSmall in buildsList) {
			count++;
			if (count > limit) {
				break;
			}
			if (buildInfoSmall.Id == sourceBuild.Id) continue;
			if (!buildInfoSmall.Id.HasValue || $"{buildInfoSmall.Id}".Equals(lastNumberStr)) {
				break;
			}
			var status = GetStatus(buildInfoSmall.Status);
			if (!query.Options.IsInitialLoad && status == BuildStatus.Success) {
				break;
			}
			var locator = GetBuildLocator(query.BuildConfig);
			locator.Id = (int)buildInfoSmall.Id.Value;
			var build = await GetBuild(clientTicket, locator);
			if (build is null) break;
			TcBuildInfo info = await GetBuildInfo(build, clientTicket);
			results.Insert(0, info);
		}
	}

	public async Task<string> GetLogs(LogsQuery logsQuery) {
		if (logsQuery.BuildInfo is not TcBuildInfo { TcId: not null } tcBuildInfo) {
			return string.Empty;
		}
		using var clientTicket = _clientFactory.Create(logsQuery.ConnectorInfo.ConnectorKey);
		return await GetLogsAsync(tcBuildInfo.TcId.Value, clientTicket, logsQuery.CancellationToken);
	}

	public BuildInfo GetNoDataPlaceholder(BuildInfoQuery query) {
		using var clientTicket = _clientFactory.Create(query.ConnectorInfo.ConnectorKey);
		var buildConfig = query.BuildConfig;
		return BuildInfo.NoData(query.BuildConfig) with {
			Url = clientTicket.Client.BaseUrl + $"/buildConfiguration/{buildConfig.Key}"
		};
	}

	private async Task<string> GetLogsAsync(long buildId, TeamCityClientTicket clientTicket,
		CancellationToken cancellationToken) {
		var client = clientTicket.Client;
		var type = typeof(TeamCityClient);
		var httpClient = (HttpClient)type.GetField("_httpClient", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(client)!;
		var address = clientTicket.Secrets.Uri + $"/httpAuth/downloadBuildLog.html?buildId={buildId}";
		return await httpClient.GetStringAsync(address, cancellationToken);
	}

	public async Task<BuildInfo> GetSingleBuildInfo(string buildConfigId, int buildId, string connectorKey) {
		using var clientTicket = _clientFactory.Create(connectorKey);
		var locator = new BuildLocator {
			BuildType = new BuildTypeLocator {
				Id = buildConfigId
			},
			Id = buildId
		};
		var build = await GetBuild(clientTicket, locator);
		var buildInfo = await GetBuildInfo(build!, clientTicket);
		return buildInfo;
	}

	private async Task<TcBuildInfo> GetBuildInfo(Build build,
			TeamCityClientTicket clientTicket) {
		var endDate = ParseDate(build.FinishOnAgentDate);
		var startDate = ParseDate(build.StartDate);
		var changes = GetChanges(build);
		var info = new TcBuildInfo(_clientFactory) {
			Id = build.Id.GetValueOrDefault(0).ToString(),
			Number = build.Number,
			TcId = build.Id,
			StartDate = startDate ?? DateTimeOffset.Now,
			Duration = endDate - startDate,
			Name = build.BuildType.Name,
			Url = build.WebUrl,
			Group = build.BuildType.ProjectName,
			BranchName = build.BranchName,
			StatusText = build.StatusText,
			Status = GetStatus(build.Status),
			Changes = changes,
			Problems = GetProblems(build),
			FailedTests = await GetFailedTests(build, clientTicket)
		};
		if (info.Status.ToString().Equals(info.StatusText, StringComparison.OrdinalIgnoreCase)) {
			info.StatusText = null;
		}
		return info;
	}

	private async Task<IReadOnlyCollection<CITestOccurence>> GetFailedTests(Build build,
			TeamCityClientTicket clientTicket) {
		var result = new List<CITestOccurence>();
		foreach (var status in new []{TestOccurrenceLocatorStatus.Error, TestOccurrenceLocatorStatus.Failure}) {
			var locator = new TestOccurrenceLocator {
				Build = new BuildLocator {
					Id = (int?)build.Id
				},
				Status = status
			};
			TestOccurrences? tests = await clientTicket.Client.TestOccurrences
				.Include(x =>x.TestOccurrence, IncludeType.Long)
				.Include(x=>x.TestCounters)
				.WithLocator(locator).GetAsync();
			if (tests.TestOccurrence is { } list ) {
				foreach (TestOccurrence? test in list) {
					var testOccurence = new CITestOccurence(test.Name) {
						TestId = test.Test.Id,
						Details = test.Details,
						Ignored = test.Ignored,
						CurrentlyMuted = test.CurrentlyMuted,
						CurrentlyInvestigated = test.CurrentlyInvestigated,
						NewFailure = test.NewFailure,
					};
					result.Add(testOccurence);
				}
			}
		}
		return result;
	}

	private IReadOnlyCollection<CIBuildProblem> GetProblems(Build build) {
		var result = new List<CIBuildProblem>();
		if (build.ProblemOccurrences?.ProblemOccurrence is { } list) {
			foreach (var problemOccurrence in list) {
				var type = problemOccurrence.Type.ToUpperInvariant() switch {
					"TC_FAILED_TESTS" => CIBuildProblemType.FailedTests,
					_ => CIBuildProblemType.Unknown
				};
				//todo problemOccurrence.Problem.Description to short summary?
				result.Add(new CIBuildProblem(type, problemOccurrence.Type,
					problemOccurrence.Details, problemOccurrence.NewFailure));
			}
		}
		return result;
	}


	private IReadOnlyCollection<VcsChange> GetChanges(Build build) {
		var changes = build.Changes.Value;
		if (!changes.Any()) {
			return ArraySegment<VcsChange>.Empty;
		}
		var res = new List<VcsChange>();
		foreach (var change in changes) {
			var files = change.Files.File.Select(f => new FileModification {
				Path = f.File,
				Type = f.ChangeType.ToLowerInvariant() switch {
					"added" => FileModificationType.Add,
					"edited" => FileModificationType.Edit,
					"removed" => FileModificationType.Delete,
					"moved" => FileModificationType.Move,
					"copied" => FileModificationType.Copy,
					_ => FileModificationType.Unknown
				}
			}).ToImmutableArray();
			var changesDate = ParseDate(change.Date);
			var user = new VcsUser(change.Username, change.User?.Name ?? change.Username, change.User?.Email);
			var item = new VcsChange(user, changesDate, change.Comment, files);
			res.Add(item);
		}
		return res;
	}

	private static AdvancedBuildLocator GetBuildLocator(BuildConfig buildConfig) {
		var buildLocator = new AdvancedBuildLocator {
			Count = 1,
			Canceled = false,
			BuildType = new BuildTypeLocator {
				Id = buildConfig.Key
			},
			Personal = false,
			Running = false,
			DefaultFilter = false
		};
		if (!buildConfig.IsDefaultBranch && !string.IsNullOrWhiteSpace(buildConfig.Branch)) {
			buildLocator.Branch = new BranchLocator {
				Name = buildConfig.Branch
			};
		}
		return buildLocator;
	}

	private static async Task<Build?> GetBuild(BuildInfoQuery infoQuery, TeamCityClientTicket clientTicket) {
		var buildConfig = infoQuery.BuildConfig;
		var buildLocator = GetBuildLocator(buildConfig);
		if (infoQuery.Options.LastBuildId is { Length: > 0 } idStr && int.TryParse(idStr, out var id)) {
			buildLocator.SinceBuild = new BuildLocator() {
				Id = id
			};
		}
		return await GetBuild(clientTicket, buildLocator);
	}

	private static async Task<Build?> GetBuild(TeamCityClientTicket clientTicket, BuildLocator buildLocator) {
		var detailedBuilds = await clientTicket.Client.Builds.Include(x => x.Build, IncludeType.Long)
			.ThenInclude(x => x.Changes, IncludeType.Long).ThenInclude(x => x.Change, IncludeType.Long)
			.ThenInclude(x => x.Attributes).Include(x => x.Build, IncludeType.Long)
			.ThenInclude(x => x.Changes, IncludeType.Long).ThenInclude(x => x.Change, IncludeType.Long)
			.ThenInclude(x => x.User).ThenInclude(x => x.Email).Include(x => x.Build, IncludeType.Long)
			.ThenInclude(x => x.ProblemOccurrences).ThenInclude(x => x.ProblemOccurrence, IncludeType.Long)
			.ThenInclude(x=>x.Problem, IncludeType.Long).ThenInclude(x=>x.Investigations)
			.ThenInclude(x=>x.Investigation, IncludeType.Long)
			.WithLocator(buildLocator).GetAsync(1);
		return detailedBuilds.Build.FirstOrDefault();
	}

	private BuildStatus GetStatus(string status) {
		return status.Equals("success", StringComparison.InvariantCultureIgnoreCase)
			? BuildStatus.Success
			: BuildStatus.Failed;
	}

}
