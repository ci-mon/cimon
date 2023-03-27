namespace Cimon.Data;

using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;

public enum CISystem
{
	TeamCity, Jenkinks
}

public class BuildLocator
{
	public CISystem CiSystem { get; set; }

	public string Id { get; set; }
}

public class BuildInfoService : IDisposable
{
	private ImmutableList<BuildLocator> _trackedLocators = ImmutableList.Create<BuildLocator>();
	private readonly BehaviorSubject<List<BuildInfo>> _teamcityBuilds = new(new List<BuildInfo>());
	private readonly BehaviorSubject<List<BuildInfo>> _jenkinsBuilds = new(new List<BuildInfo>());
	public IObservable<IList<BuildInfo>> Watch(List<BuildLocator> builds) {
		return _teamcityBuilds.CombineLatest(_jenkinsBuilds)
			.Select(x => x.First.Concat(x.Second).Where(i => builds.Any(b => b.Id == i.BuildId)).ToList());
	}

	public void Stop() {
		_teamcityBuilds.OnCompleted();
		_jenkinsBuilds.OnCompleted();
	}

	public void OnTcStatusChange() {
		_teamcityBuilds.OnNext(new List<BuildInfo> {
			new BuildInfo() {
				Name = "test",
				BuildId = "testId"
			}
		});
	}

	/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
	public void Dispose() {
		_teamcityBuilds.Dispose();
		_jenkinsBuilds.Dispose();
	}
}
