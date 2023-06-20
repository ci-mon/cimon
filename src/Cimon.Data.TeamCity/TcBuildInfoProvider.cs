using Cimon.Contracts;
using Cimon.Contracts.Services;
using Microsoft.Extensions.Logging;
using TeamCityAPI.Locators.Enums;
using TeamCityAPI.Queries;
using TeamCityAPI.Queries.Common;
using BuildStatus = Cimon.Contracts.BuildStatus;

namespace Cimon.Data.TeamCity;

public class TcBuildInfoProvider : IBuildInfoProvider
{

	private readonly ILogger _logger;
	private readonly TcClient _client;
	public TcBuildInfoProvider(TcClient client, ILogger<TcBuildInfoProvider> logger) {
		_client = client;
		_logger = logger;
	}
	public CISystem CiSystem => CISystem.TeamCity;

	public async Task<IReadOnlyCollection<BuildInfo>> GetInfo(IReadOnlyList<BuildConfigInfo> buildConfigs) {
		using var clientTicket = _client.GetClient();
		List<BuildInfo?> list = new List<BuildInfo?>();
		foreach (var l in buildConfigs) {
			var detailedBuilds = await clientTicket.Client.Builds
				.Include(x => x.Build, IncludeType.Long).ThenInclude(x => x.Changes, IncludeType.Long).ThenInclude(x=>x.Change, IncludeType.Long).ThenInclude(x=>x.Attributes)
				.WithLocator(new TeamCityAPI.Locators.BuildLocator {
					Count = 10,
					BuildType = new TeamCityAPI.Locators.BuildTypeLocator {
						Id = l.Key
					},
					Personal = false,
					Running = false
				}).GetAsync();
			var build = detailedBuilds.Build.FirstOrDefault();
			if (build is null) continue;
			var committers = build.Changes.Change.Select(x=>x.Username).ToList();
			var info = new TcBuildInfo(_client) {
				/*StartDate = DateTimeOffset.Parse(build.StartDate),
				FinishDate = DateTimeOffset.Parse(build.FinishDate),*/
				Name = build.BuildType.Name,
				BuildConfigId = l.Key,
				BuildHomeUrl = build.WebUrl,
				ProjectName = build.BuildType.ProjectName,
				Number = build.Number,
				StatusText = build.StatusText,
				Status = GetStatus(build.Status),
				BranchName = build.BranchName,
				Committers = committers,
			};
			info.AddInvestigationActions(build);
			list.Add(info);
		}
		return (IReadOnlyCollection<BuildInfo>)list;
	}
	private BuildStatus GetStatus(string status) {
		return status.Equals("success", StringComparison.InvariantCultureIgnoreCase)
			? BuildStatus.Success
			: BuildStatus.Failed;
	}

}
