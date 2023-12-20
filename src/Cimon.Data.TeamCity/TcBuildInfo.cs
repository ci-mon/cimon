using TeamCityAPI.Models;

namespace Cimon.Data.TeamCity;

using Contracts.CI;

record TcBuildInfo : BuildInfo, IBuildInfoActionsProvider
{
	private readonly TcClientFactory _clientFactory;
	private readonly List<BuildInfoActionDescriptor> _actions = new();
	public TcBuildInfo(TcClientFactory clientFactory) {
		_clientFactory = clientFactory;
	}

	public long? TcId { get; set; }

	public void AddInvestigationActions(Build build) {
		var committers = build.Changes.Change.Select(x=>x.Username).Distinct().ToList();
		foreach (var user in committers) {
			_actions.Add(new BuildInfoActionDescriptor {
				Id = Guid.NewGuid(),
				GroupDescription = "Assign investigation to:",
				Description = $"[{user}]",
				Execute = () => AddInvestigation(build, user)
			});
		}
	}

	private Task AddInvestigation(Build build, string user) {
		throw new NotImplementedException();
	}

	public IReadOnlyCollection<BuildInfoActionDescriptor> GetAvailableActions() => _actions;
}