using Cimon.Contracts;
using Cimon.Data.BuildInformation;

namespace Cimon.Data.TeamCity;

public class TcBuildLocatorProvider : IBuildLocatorProvider
{

	public async IAsyncEnumerable<BuildLocatorDescriptor> GetLocators() {
		foreach (BuildInfo x in MockData.TestBuildInfos) yield return new BuildLocatorDescriptor {
			Id = x.Name,
			CiSystem = CISystem.TeamCity,
			Path = x.ProjectName
		};
	}
}
