using Cimon.Contracts;
using Cimon.Contracts.Services;

namespace Cimon.Data.Jenkins;

public class JenkinsBuildConfigProvider : IBuildConfigProvider
{
	private readonly ClientFactory _clientFactory;
	public JenkinsBuildConfigProvider(ClientFactory clientFactory) {
		_clientFactory = clientFactory;
	}

	public async Task<IReadOnlyCollection<BuildConfigInfo>> GetAll() {
		using var client = _clientFactory.Create();
		var result = new List<BuildConfigInfo>();
		var master = await client.GetMaster(default);
		foreach (var job in master.Jobs) {
			var jobInfo = await client.GetJob(job.Name, default);
			if (!jobInfo.Jobs.Any()) {
				if (jobInfo.Buildable) {
					result.Add(new BuildConfigInfo(job.Name) {
						Props = {
							{nameof(job.Url), job.Url.ToString()}
						}
					});
				}
				continue;
			}
			foreach (var infoJob in jobInfo.Jobs) {
				var isDefault = infoJob.Name == "master";
				result.Add(new BuildConfigInfo(job.Name, infoJob.Name, isDefault) {
					Props = {
						{nameof(job.Url), infoJob.Url.ToString()}
					}
				});
			}


		}
		return result;
	}

	public CISystem CISystem => CISystem.Jenkins;
}