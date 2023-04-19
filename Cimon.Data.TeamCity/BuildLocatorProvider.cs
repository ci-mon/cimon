namespace Cimon.Data.TeamCity;

public class TcBuildLocatorProvider : IBuildLocatorProvider
{

	public async IAsyncEnumerable<BuildLocatorDescriptor> GetLocators() {
		for (int i = 0; i < 10; i++) {
			yield return new BuildLocatorDescriptor {
				Id = i.ToString()
			};
		}
	}
}
