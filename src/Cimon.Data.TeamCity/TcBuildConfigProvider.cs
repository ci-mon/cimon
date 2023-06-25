using Cimon.Contracts;
using Cimon.Contracts.Services;
using TeamCityAPI.Locators;
using TeamCityAPI.Models;
using TeamCityAPI.Queries;
using TeamCityAPI.Queries.Common;
using TeamCityAPI.Queries.Interfaces;

namespace Cimon.Data.TeamCity;

public class TcBuildConfigProvider : IBuildConfigProvider
{

	private readonly TcClientFactory _clientFactory;
	public TcBuildConfigProvider(TcClientFactory clientFactory) {
		_clientFactory = clientFactory;
	}

	public async Task<IReadOnlyCollection<BuildConfigInfo>> GetAll() {
		var results = new List<BuildConfigInfo>();
		await foreach (var (buildConfig, branches) in GetBuildConfigs()) {
			if (!branches.Any()) {
				var item = new BuildConfigInfo(buildConfig.Id, null, true) {
					Props = new Dictionary<string, string> {
						{"ProjectName", buildConfig.ProjectName}
					}
				};
				results.Add(item);
				continue;
			}
			foreach (var branch in branches) {
				if(branch.Default != true && branch.Active == false) continue;
				if(branch.Active == false) continue;
				var branchName = branch.Name;
				if (branchName?.Equals("<default>", StringComparison.OrdinalIgnoreCase) == true) {
					branchName = null;
				}
				var item = new BuildConfigInfo(buildConfig.Id, branchName, branch.Default ?? false) {
					Props = new Dictionary<string, string> {
						{"ProjectName", buildConfig.ProjectName}
					}
				};
				results.Add(item);
			}
		}
		return results;
	}

	private async IAsyncEnumerable<(BuildType, IReadOnlyCollection<Branch>)> GetBuildConfigs() {
		using var client = _clientFactory.GetClient();
		var configs = client.Client.BuildTypes
			.Include(x => x.BuildType).ThenInclude(x=>x.Branches, IncludeType.Long)
			.GetAsyncEnumerable<BuildTypes, BuildType>()
			.Select(buildType => buildType);
		await foreach (var buildType in configs) {
			var locator = new BuildTypeLocator {
				Id = buildType.Id
			}.ToString();
			var branches = await client.Client.GetAllBranchesOfBuildTypeAsync(locator, null, null);
			var collection = branches.Branch?.ToList() as IReadOnlyCollection<Branch> ?? ArraySegment<Branch>.Empty;
			yield return (buildType, collection);
		}
	}

	public CISystem CISystem => CISystem.TeamCity;
}
