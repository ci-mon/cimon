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
		foreach (var infoItem in _buffer.IterateReversed()) {
			if (infoItem.Info.Status == BuildStatus.Success) {
				break;
			}
			if (!infoItem.Resolved) {
				if (TryResolveOldBuildsByProblems(infoItem, newInfo, currentFailedTests)) continue;
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

	private static bool TryResolveOldBuildsByProblems(BuildInfoItem oldInfoItem, BuildInfo newInfo,
			HashSet<string> currentFailedTests) {
		var oldFailedTests = oldInfoItem.Info.FailedTests;
		var oldProblems = oldInfoItem.Info.Problems.ToHashSet();
		if (oldFailedTests.Count == 0 && currentFailedTests.Count == 0 && oldProblems.Count == 0 &&
				newInfo.Problems.Count == 0) {
			if (!string.Equals(oldInfoItem.Info.StatusText, newInfo.StatusText,
					StringComparison.InvariantCultureIgnoreCase)) {
				oldInfoItem.Resolved = true;
				return true;
			}
		}
		if (oldFailedTests.Count == 0) {
			if (!oldProblems.Overlaps(newInfo.Problems)) {
				oldInfoItem.Resolved = true;
				return true;
			}
		} else {
			if (currentFailedTests.Any() && oldFailedTests.All(t => !currentFailedTests.Contains(t.TestId))) {
				oldInfoItem.Resolved = true;
				return true;
			}
		}
		return false;
	}

	public bool SetFailureSuspect(string buildInfoId, BuildFailureSuspect failureSuspect) {
		bool suspectFound = false;
		foreach (var infoItem in _buffer) {
			if (suspectFound) {
				if (infoItem.Info.Status == BuildStatus.Success) {
					break;
				}
				infoItem.Info.FailureSuspect = failureSuspect;
			}
			if (infoItem.Info.Id == buildInfoId) {
				infoItem.Info.FailureSuspect = failureSuspect;
				suspectFound = true;
			}
		}
		return Last?.Id == buildInfoId;
	}
}
