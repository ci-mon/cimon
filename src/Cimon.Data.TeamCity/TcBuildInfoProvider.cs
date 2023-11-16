using System.Collections.Immutable;
using Cimon.Contracts.Services;
using Microsoft.Extensions.Logging;
using TeamCityAPI.Locators;
using TeamCityAPI.Models;
using TeamCityAPI.Queries;
using TeamCityAPI.Queries.Common;

namespace Cimon.Data.TeamCity;

using Cimon.Contracts.CI;
using TeamCityAPI.Locators.Enums;

public class TcBuildInfoProvider : IBuildInfoProvider
{

	private readonly ILogger _logger;
	private readonly TcClientFactory _clientFactory;

	public TcBuildInfoProvider(TcClientFactory clientFactory, ILogger<TcBuildInfoProvider> logger) {
		_clientFactory = clientFactory;
		_logger = logger;
	}

	public CISystem CiSystem => CISystem.TeamCity;

	private readonly string _dateFormat = "yyyyMMdd'T'HHmmsszzz";

	private DateTimeOffset? ParseDate(string teamcityDate) {
		if (string.IsNullOrWhiteSpace(teamcityDate)) {
			return null;
		}
		return DateTimeOffset.ParseExact(teamcityDate, _dateFormat, null);
	}

	public async Task<IReadOnlyCollection<BuildInfo>> GetInfo(IReadOnlyList<BuildInfoQuery> infoQueries) {
		using var clientTicket = _clientFactory.GetClient();
		List<BuildInfo?> list = new List<BuildInfo?>();
		foreach (var buildInfoQuery in infoQueries) {
			var buildConfig = buildInfoQuery.BuildConfig;
			var buildConfigId = buildConfig.Key;
			var build = await GetBuild(buildConfig, clientTicket);
			if (build is null)
				continue;
			TcBuildInfo info = await GetBuildInfo(build, buildConfigId, clientTicket);
			string? lastBuildNumber = buildInfoQuery.Options?.LastBuildNumber;
			if (!string.IsNullOrWhiteSpace(lastBuildNumber) && info.Status == BuildStatus.Failed &&
					build.Id is { } buildId) {
				info.Log = await _clientFactory.GetLogsAsync(buildId);
			}
			info.AddInvestigationActions(build);
			list.Add(info);
		}
		return (IReadOnlyCollection<BuildInfo>)list;
	}

	public async Task<BuildInfo> GetSingleBuildInfo(string buildConfigId, int buildId) {
		using var clientTicket = _clientFactory.GetClient();
		var locator = new BuildLocator {
			BuildType = new BuildTypeLocator {
				Id = buildConfigId
			},
			Id = buildId
		};
		var build = await GetBuild(clientTicket, locator);
		var buildInfo = await GetBuildInfo(build, buildConfigId, clientTicket);
		return buildInfo;
	}

	private async Task<TcBuildInfo> GetBuildInfo(Build build, string buildConfigId, 
			TeamCityClientTicket clientTicket) {
		var endDate = ParseDate(build.FinishOnAgentDate);
		var startDate = ParseDate(build.StartDate);
		var changes = GetChanges(build);
		var info = new TcBuildInfo(_clientFactory) {
			StartDate = startDate ?? DateTimeOffset.Now,
			Duration = endDate - startDate,
			Name = build.BuildType.Name,
			BuildConfigId = buildConfigId,
			Url = build.WebUrl,
			Group = build.BuildType.ProjectName,
			BranchName = build.BranchName,
			Number = build.Number,
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
				result.Add(new CIBuildProblem(type, problemOccurrence.Id, 
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

	private static async Task<Build?> GetBuild(BuildConfigInfo buildConfig, TeamCityClientTicket clientTicket) {
		var buildLocator = new BuildLocator {
			Count = 1,
			Canceled = false,
			BuildType = new BuildTypeLocator {
				Id = buildConfig.Key
			},
			Personal = false,
			Running = false
		};
		if (!string.IsNullOrWhiteSpace(buildConfig.Branch)) {
			buildLocator.Branch = new BranchLocator {
				Name = buildConfig.Branch
			};
		} else if (buildConfig.IsDefaultBranch) {
			buildLocator.Branch = new BranchLocator {
				Default = "true"
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
			.WithLocator(buildLocator).GetAsync(1);
		return detailedBuilds.Build.FirstOrDefault();
	}

	private BuildStatus GetStatus(string status) {
		return status.Equals("success", StringComparison.InvariantCultureIgnoreCase)
			? BuildStatus.Success
			: BuildStatus.Failed;
	}

}
