using Cimon.Contracts;
using Cimon.Contracts.Services;

namespace Cimon.Data.Jenkins;

public class JenkinsBuildConfigProvider : IBuildConfigProvider
{
	private readonly ClientFactory _clientFactory;
	public JenkinsBuildConfigProvider(ClientFactory clientFactory) {
		_clientFactory = clientFactory;
	}

	public Task<IReadOnlyCollection<BuildConfigInfo>> GetAll() {
		var client = _clientFactory.Create();
		var result = new List<BuildConfigInfo>();
		foreach (var job in client.Jobs()) {
			result.Add(new BuildConfigInfo(job.Name) {
				Props = {
					{nameof(job.Class), job.Class}
				}
			});
		}
		return Task.FromResult((IReadOnlyCollection<BuildConfigInfo>)result);
	}

	public CISystem CISystem => CISystem.Jenkins;
}