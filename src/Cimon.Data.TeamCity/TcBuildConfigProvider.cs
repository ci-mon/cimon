using Cimon.Contracts;
using Cimon.Contracts.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TeamCityAPI.Locators;
using TeamCityAPI.Models;
using TeamCityAPI.Queries;
using TeamCityAPI.Queries.Common;

namespace Cimon.Data.TeamCity;

using Contracts.CI;

public class TcBuildConfigProvider : IBuildConfigProvider
{

	private readonly TcClientFactory _clientFactory;
	private readonly string _searchedProjectsSettingKey = "buildConfigFilter_ProjectId";

	public TcBuildConfigProvider(TcClientFactory clientFactory) {
		_clientFactory = clientFactory;
	}

	public async Task<IReadOnlyCollection<BuildConfig>> GetAll(CIConnectorInfo info) {
		using var client = _clientFactory.Create(info.ConnectorKey);
		var results = new List<BuildConfig>();
		await foreach (var (buildConfig, branches) in GetBuildConfigs(client, info.Settings)) {
			if (!branches.Any()) {
				var item = new BuildConfig {
					Name = buildConfig.Name,
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
					Name = buildConfig.Name,
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

	private StringMatcher CreateMatcher(IReadOnlyDictionary<string, string> dictionary, string key, string separator) {
		if (dictionary.TryGetValue(key, out var value)) {
			return new StringMatcher(value, separator);
		}
		return StringMatcher.AnyString;
	}

	private async IAsyncEnumerable<(BuildType, IReadOnlyCollection<Branch>)> GetBuildConfigs(
		TeamCityClientTicket client, IReadOnlyDictionary<string, string> settings) {
		var configs = client.Client
			.BuildTypes
			.Include(x => x.BuildType).ThenInclude(x=>x.Branches, IncludeType.Long)
			.GetAsyncEnumerable<BuildTypes, BuildType>()
			.Select(buildType => buildType);
		var projectNameMatcher = CreateMatcher(settings, _searchedProjectsSettingKey, ";");
		await foreach (var buildType in configs) {
			if (!projectNameMatcher.Check(buildType.ProjectId)) {
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

	public Dictionary<string, string> GetSettings() {
		return new() {
			{_searchedProjectsSettingKey, "*"}
		};
	}

	public async Task<HealthCheckResult> CheckHealth(CIConnectorInfo info) {
		try {
			using var client = _clientFactory.Create(info.ConnectorKey);
			await client.Client.GetApiVersionAsync();
			return HealthCheckResult.Healthy(client.Client.BaseUrl);
		} catch (Exception e) {
			return HealthCheckResult.Unhealthy(e.Message);
		}
	}
}
