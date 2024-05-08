using System.Collections.Immutable;
using Cimon.Contracts.CI;
using Cimon.Data.Common;

namespace Cimon.Data.BuildInformation;

public class BuildInfoHistory
{
	public record Item(BuildInfo Info, bool Resolved, ImmutableList<BuildFailureSuspect> Suspects)
	{
		public bool Resolved { get; set; } = Resolved;
		public ImmutableList<BuildFailureSuspect> Suspects { get; set; } = Suspects;

		public void SetResolved() {
			Resolved = true;
			Info.FailedTests = ArraySegment<CITestOccurence>.Empty;
		}
	}

	private readonly RingBuffer<Item> _buffer = new(50);
	private BuildInfo? _actualBuildInfo;

	public BuildInfo? Last => _actualBuildInfo ?? InitializeLastBuildInfo();
	public Item Add(BuildInfo newInfo) {
		var buildInfoItem = new Item(newInfo, false, ImmutableList<BuildFailureSuspect>.Empty);
		_buffer.Add(buildInfoItem);
		_actualBuildInfo = null;
		return buildInfoItem;
	}

	public bool SetFailureSuspect(string buildInfoId, ImmutableList<BuildFailureSuspect> suspects) {
		foreach (var item in _buffer.IterateReversed()) {
			if (item.Info.Id != buildInfoId) continue;
			item.Suspects = suspects;
			_actualBuildInfo = null;
			return true;
		}
		return false;
	}

	private BuildInfo? InitializeLastBuildInfo() {
		if (_buffer.Items.Count == 0) return null;
		var items = _buffer.ToArray().AsSpan();
		var committers = GetCommittersInfo(items);
		_actualBuildInfo = items[^1].Info with {
			CombinedCommitters = committers
		};
		return _actualBuildInfo;
	}

	private IReadOnlyCollection<CommitterInfo> GetCommittersInfo(Span<Item> items) {
		var changes = new List<VcsChange>();
		var suspects = new Dictionary<VcsUser, float>();
		for (int i = items.Length - 1; i >= 0; i--) {
			var item = items[i];
			if (item.Resolved) break;
			var nextItemIndex = i + 1;
			var isItemResolved = GetIsItemResolved(item, items[nextItemIndex..]);
			if (isItemResolved) {
				item.SetResolved();
				MarkPreviousItemsAsResolved(items, i);
				break;
			}
			changes.AddRange(item.Info.Changes);
			MergeSuspectConfidence(item, suspects);
		}
		var committers = changes
			.GroupBy(x => x.Author)
			.Select(g =>
				new CommitterInfo(g.Key, g.Count(), suspects.GetValueOrDefault(g.Key, 0f)))
			.OrderByDescending(x => x.SuspectConfidence)
			.ThenByDescending(x => x.CommitsCount)
			.ToImmutableList();
		return committers;
	}

	private static void MergeSuspectConfidence(Item item, Dictionary<VcsUser, float> suspects) {
		foreach (var suspect in item.Suspects) {
			if (!suspects.TryGetValue(suspect.User, out var currentConfidence)) {
				currentConfidence = 0;
			}
			suspects[suspect.User] = currentConfidence + suspect.Confidence;
		}
	}

	private static void MarkPreviousItemsAsResolved(Span<Item> items, int i) {
		for (int j = i - 1; j >= 0; j--) {
			var prev = items[j];
			if (!prev.Resolved) {
				prev.SetResolved();
			}
		}
	}

	private bool GetIsItemResolved(Item item, Span<Item> nextItems) {
		if (item.Info.Status == BuildStatus.Success) return true;
		if (nextItems.IsEmpty) return false;
		(CIBuildProblemType Type, string ShortSummary, string Details) GetProblemInfo(CIBuildProblem x) =>
			(x.Type, x.ShortSummary, x.Details);
		var unresolvedProblems = item.Info.Problems.Select(GetProblemInfo).ToHashSet();
		foreach (var nextItem in nextItems) {
			unresolvedProblems = unresolvedProblems.Intersect(nextItem.Info.Problems.Select(GetProblemInfo)).ToHashSet();
			if (unresolvedProblems.Count == 0) break;
		}
		(string Name, string TestId, string Details) GetTestInfo(CITestOccurence t) => (t.Name, t.TestId, t.Details);
		bool GetIsTestReallyFailing(CITestOccurence x) => x.Ignored is not true && x.CurrentlyMuted is not true;
		var failedTests = item.Info.FailedTests.Where(GetIsTestReallyFailing).Select(GetTestInfo).ToHashSet();
		foreach (var nextItem in nextItems) {
			failedTests = failedTests
				.Intersect(nextItem.Info.FailedTests.Where(GetIsTestReallyFailing).Select(GetTestInfo)).ToHashSet();
			if (failedTests.Count == 0) break;
		}
		return unresolvedProblems.Count == 0 && failedTests.Count == 0;
	}
}
