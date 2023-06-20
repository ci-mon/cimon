using Cimon.Contracts;
using TeamCityAPI.Models;

namespace Cimon.Data.TeamCity;

record TcBuildInfo : BuildInfo, IBuildInfoActionsProvider
{
	private readonly TcClient _client;
	private readonly List<BuildInfoActionDescriptor> _actions = new();
	public TcBuildInfo(TcClient client) {
		_client = client;
	}

	public void AddInvestigationActions(Build build) {
		var committers = build.Changes.Change.Select(x=>x.User).ToList();
		foreach (var user in committers) {
			_actions.Add(new BuildInfoActionDescriptor {
				Id = Guid.NewGuid(),
				GroupDescription = "Assign investigation to:",
				Description = $"[{user.Username}]",
				Execute = () => AddInvestigation(build, user)
			});
		}
	}

	private Task AddInvestigation(Build build, TeamCityAPI.Models.User user) {
		throw new NotImplementedException();
	}

	public IReadOnlyCollection<BuildInfoActionDescriptor> GetAvailableActions() => _actions;
}