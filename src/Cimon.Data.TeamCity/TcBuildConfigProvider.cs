using Cimon.Contracts.Services;
using TeamCityAPI.Locators;
using TeamCityAPI.Models;
using TeamCityAPI.Queries;
using TeamCityAPI.Queries.Common;

namespace Cimon.Data.TeamCity;

using Contracts.CI;

public class TcBuildConfigProvider : IBuildConfigProvider
{

	private readonly TcClientFactory _clientFactory;
	public TcBuildConfigProvider(TcClientFactory clientFactory) {
		_clientFactory = clientFactory;
	}

	public async Task<IReadOnlyCollection<BuildConfig>> GetAll(CIConnectorInfo info) {
		var results = new List<BuildConfig>();
		await foreach (var (buildConfig, branches) in GetBuildConfigs(info.Settings)) {
			if (!branches.Any()) {
				var item = new BuildConfig {
					Key = buildConfig.Id,
					Props = new Dictionary<string, string> {
						{"ProjectId", buildConfig.ProjectId}
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
				var item = new BuildConfig {
					Key = buildConfig.Id,
					Branch = branchName,
					IsDefaultBranch = branch.Default ?? false,
					Props = new Dictionary<string, string> {
						{"ProjectId", buildConfig.ProjectId}
					}
				};
				results.Add(item);
			}
		}
		return results;
	}

	private async IAsyncEnumerable<(BuildType, IReadOnlyCollection<Branch>)> GetBuildConfigs(
			IReadOnlyDictionary<string, string> settings) {
		using var client = _clientFactory.GetClient();
		var configs = client.Client
			.BuildTypes
			.Include(x => x.BuildType).ThenInclude(x=>x.Branches, IncludeType.Long)
			.GetAsyncEnumerable<BuildTypes, BuildType>()
			.Select(buildType => buildType);
		// TODO filter projects, user project key
		var allowedProjects = new List<string> {
			"ContinuousIntegration_ProductsDiagnostics",
			"ContinuousIntegration_UnitTest_C"
		};
		await foreach (var buildType in configs) {
			if (!allowedProjects.Any(x => buildType.ProjectId.StartsWith(x, StringComparison.OrdinalIgnoreCase))) {
				continue;
			}
			var locator = new BuildTypeLocator {
				Id = buildType.Id
			}.ToString();
			var branches = await client.Client.GetAllBranchesOfBuildTypeAsync(locator, null, null);
			var collection = branches.Branch?.ToList() as IReadOnlyCollection<Branch> ?? ArraySegment<Branch>.Empty;
			yield return (buildType, collection);
		}
	}

	public CISystem CISystem => CISystem.TeamCity;
	public Dictionary<string, string> GetSettings() {
		return new() {
			{"searched_projects", "*"}
		};
	}
}
