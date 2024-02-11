using Cimon.Contracts.Services;

namespace Cimon.Data.Jenkins;

using Contracts.CI;

public class JenkinsBuildConfigProvider(ClientFactory clientFactory) : IBuildConfigProvider
{
	public async Task<IReadOnlyCollection<BuildConfig>> GetAll(CIConnectorInfo info) {
		using var client = clientFactory.Create(info.ConnectorKey);
		var result = new List<BuildConfig>();
		var master = await client.GetMaster(default);
		foreach (var job in master.Jobs) {
			var jobInfo = await client.GetJob(job.Name, default);
			if (!jobInfo.Jobs.Any()) {
				if (jobInfo.Buildable) {
					result.Add(new BuildConfig {
						Key = job.Name,
						Props = {
							{nameof(job.Url), job.Url.ToString()}
						}
					});
				}
				continue;
			}
			foreach (var infoJob in jobInfo.Jobs) {
				var isDefault = infoJob.Name == "master";
				result.Add(new BuildConfig() {
					Key = job.Name,
					Branch = infoJob.Name,
					IsDefaultBranch = isDefault,
					Props = {
						{nameof(job.Url), infoJob.Url.ToString()}
					}
				});
			}
		}
		return result;
	}

	public Dictionary<string, string> GetSettings() => new();
}
