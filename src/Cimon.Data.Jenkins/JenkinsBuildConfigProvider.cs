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
			result.Add(new BuildConfigInfo(job.Name) {
				Props = {
					{nameof(job.Url), job.Url.ToString()}
				}
			});
		}
		return result;
	}

	public CISystem CISystem => CISystem.Jenkins;
}