using System.Collections.Immutable;
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
		var buildInfoItem = new BuildInfoItem(newInfo, false);
		if (newInfo.Status == BuildStatus.Failed) {
			var committers = newInfo.Changes.Select(c => c.Author).ToHashSet();
			if (committers.Count == 1) {
				newInfo.FailureSuspects = ImmutableList.Create(new BuildFailureSuspect(committers.First(), 50));
			}
		}
		if (changes.Any()) {
			changes.AddRange(newInfo.Changes);
			newInfo.Changes = changes;
		}
		if (_buffer.Last is { Resolved: false } last) {
			SetSuspects(buildInfoItem, last.Info.FailureSuspects);
		}
		_buffer.Add(buildInfoItem);
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

	public bool SetFailureSuspect(string buildInfoId, ImmutableList<BuildFailureSuspect> failureSuspects) {
		bool suspectFound = false;
		foreach (var infoItem in _buffer) {
			if (suspectFound) {
				if (infoItem.Info.Status == BuildStatus.Success) {
					break;
				}
				SetSuspects(infoItem, failureSuspects);
			}
			if (infoItem.Info.Id == buildInfoId) {
				SetSuspects(infoItem, failureSuspects);
				suspectFound = true;
			}
			if (infoItem.Resolved) {
				break;
			}
		}
		return Last?.Id == buildInfoId;
	}

	private void SetSuspects(BuildInfoItem infoItem, ImmutableList<BuildFailureSuspect>? failureSuspects) {
		if (failureSuspects is null) return;
		var currentSuspects = infoItem.Info.FailureSuspects;
		if (currentSuspects is null) {
			infoItem.Info.FailureSuspects = failureSuspects;
			return;
		}
		foreach (var suspect in failureSuspects) {
			if (currentSuspects.FirstOrDefault(s=>s.User == suspect.User) is { } current) {
				if (current.Confidence < suspect.Confidence) {
					currentSuspects = currentSuspects.Remove(current).Add(suspect);
				}
			} else {
				currentSuspects = currentSuspects.Add(suspect);
			}
		}
		infoItem.Info.FailureSuspects = currentSuspects;
	}
}
