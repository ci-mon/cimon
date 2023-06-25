using System.Collections.Immutable;
using System.Reflection;
using Cimon.Contracts;
using Cimon.Contracts.Services;
using Microsoft.Extensions.Logging;
using TeamCityAPI.Locators;
using TeamCityAPI.Locators.Enums;
using TeamCityAPI.Models;
using TeamCityAPI.Queries;
using TeamCityAPI.Queries.Common;
using BuildStatus = Cimon.Contracts.BuildStatus;

namespace Cimon.Data.TeamCity;

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
	private DateTimeOffset ParseDate(string teamcityDate) {
		return DateTimeOffset.ParseExact(teamcityDate, _dateFormat, null);
	}

	public async Task<IReadOnlyCollection<BuildInfo>> GetInfo(IReadOnlyList<BuildInfoQuery> infoQueries) {
		using var clientTicket = _clientFactory.GetClient();
		List<BuildInfo?> list = new List<BuildInfo?>();
		foreach (var buildInfoQuery in infoQueries) {
			var buildConfig = buildInfoQuery.BuildConfig;
			var buildConfigId = buildConfig.Key;
			var build = await GetBuild(buildConfig, clientTicket);
			if (build is null) continue;
			var committers = build.Changes.Change.Select(x=>x.Username).Distinct().ToList();
			var endDate = ParseDate(build.FinishOnAgentDate);
			var startDate = ParseDate(build.StartDate);
			var info = new TcBuildInfo(_clientFactory) {
				StartDate = startDate,
				Duration = endDate - startDate,
				Name = build.BuildType.Name,
				BuildConfigId = buildConfigId,
				Url = build.WebUrl,
				Group = build.BuildType.ProjectName,
				BranchName = build.BranchName,
				Number = build.Number,
				StatusText = build.StatusText,
				Status = GetStatus(build.Status),
				Committers = committers,
				Changes = GetChanges(build)
			};
			if (info.Status.ToString().Equals(info.StatusText, StringComparison.OrdinalIgnoreCase)) {
				info.StatusText = null;
			}
			var lastBuildNumber = buildInfoQuery.Options?.LastBuildNumber;
			if (!string.IsNullOrWhiteSpace(lastBuildNumber) && info.Status == BuildStatus.Failed && build.Id is { } buildId) {
				info.Log = await _clientFactory.GetLogsAsync(buildId);
			}
			info.AddInvestigationActions(build);
			list.Add(info);
		}
		return (IReadOnlyCollection<BuildInfo>)list;
	}

	private IReadOnlyCollection<VCSChange> GetChanges(Build build) {
		var changes = build.Changes.Value;
		if (!changes.Any()) {
			return ArraySegment<VCSChange>.Empty;
		}
		var res = new List<VCSChange>();
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
			var item = new VCSChange(change.Username, changesDate, change.Comment, files);
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
		}
		else if (buildConfig.IsDefaultBranch) {
			buildLocator.Branch = new BranchLocator {
				Default = "true"
			};
		}
		var detailedBuilds = await clientTicket.Client.Builds
			.Include(x => x.Build, IncludeType.Long)
				.ThenInclude(x => x.Changes, IncludeType.Long)
						.ThenInclude(x => x.Change, IncludeType.Long)
							.ThenInclude(x => x.Attributes)
			.Include(x => x.Build, IncludeType.Long)
				.ThenInclude(x => x.Changes, IncludeType.Long)
						.ThenInclude(x => x.Change, IncludeType.Long)
							.ThenInclude(x => x.User)
			.WithLocator(buildLocator).GetAsync(1);
		return detailedBuilds.Build.FirstOrDefault();
	}

	private BuildStatus GetStatus(string status) {
		return status.Equals("success", StringComparison.InvariantCultureIgnoreCase)
			? BuildStatus.Success
			: BuildStatus.Failed;
	}

}
