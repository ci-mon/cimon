using Cimon.Contracts.CI;
using Cimon.Data.Common;

namespace Cimon.Data.BuildInformation;

public class BuildInfoHistory
{
	record BuildInfoItem(BuildInfo Info, bool Resolved)
	{
		public bool Resolved { get; set; } = Resolved;
	}

	private readonly RingBuffer<BuildInfoItem> _buffer = new(50);
	public BuildInfo? Last => _buffer.Last?.Info;
	public void Add(BuildInfo newInfo) {
		var changes = new List<VcsChange>();
		var currentFailedTests = newInfo.FailedTests.Select(x => x.TestId).ToHashSet();
		var currentProblems = newInfo.Problems.Select(x => (x.Type, x.Details)).ToHashSet();
		foreach (var infoItem in _buffer.IterateReversed()) {
			if (!infoItem.Resolved) {
				if (TryResolveOldBuilds(infoItem, currentProblems, currentFailedTests)) continue;
				changes.InsertRange(0,
					infoItem.Info.Changes.Where(x => !x.IsInherited).Select(x => x with { IsInherited = true }));
			}
		}
		if (changes.Any()) {
			changes.AddRange(newInfo.Changes);
			newInfo.Changes = changes;
		}
		_buffer.Add(new BuildInfoItem(newInfo, false));
	}

	private static bool TryResolveOldBuilds(BuildInfoItem infoItem, HashSet<(CIBuildProblemType Type, string Details)> currentProblems, HashSet<string> currentFailedTests) {
		var oldFailedTests = infoItem.Info.FailedTests;
		var oldProblems = infoItem.Info.Problems.Select(x => (x.Type, x.Details)).ToHashSet();
		if (oldFailedTests.Count == 0) {
			if (!currentProblems.Overlaps(oldProblems)) {
				infoItem.Resolved = true;
				return true;
			}
		} else {
			if (currentFailedTests.Any() && oldFailedTests.All(t => !currentFailedTests.Contains(t.TestId))) {
				infoItem.Resolved = true;
				return true;
			}
		}
		return false;
	}
}
