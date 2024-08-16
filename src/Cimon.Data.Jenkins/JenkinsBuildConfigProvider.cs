using Cimon.Contracts.Services;
using Cimon.Jenkins;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cimon.Data.Jenkins;

using Contracts.CI;

public class JenkinsBuildConfigProvider(ClientFactory clientFactory) : IBuildConfigProvider
{
	public async Task<IReadOnlyCollection<BuildConfig>> GetAll(CIConnectorInfo info, Action<int>? reportProgress) {
		using var client = clientFactory.Create(info.ConnectorKey);
		var result = new List<BuildConfig>();
		var master = await client.Query(new JenkinsApi.Master());
		if (master is null) return result;
		int jobNumber = 0;
		foreach (var job in master.Jobs) {
			jobNumber++;
			var progress = Convert.ToInt32(Math.Round(jobNumber * 100d / master.Jobs.Count, 0d));
			reportProgress?.Invoke(progress);
			var jobInfo = await client.Query(new JenkinsApi.Job(JobLocator.Create(job.Name)));
			if (jobInfo is null) continue;
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
	public async Task<HealthCheckResult> CheckHealth(CIConnectorInfo info) {
		try {
			using var client = clientFactory.Create(info.ConnectorKey, out var config);
			await client.Query(new JenkinsApi.Master());
			return HealthCheckResult.Healthy(config.JenkinsUrl.ToString());
		} catch (Exception e) {
			return HealthCheckResult.Unhealthy(e.Message);
		}
	}
}
