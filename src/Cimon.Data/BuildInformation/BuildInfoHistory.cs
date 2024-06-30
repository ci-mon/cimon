using System.Collections.Immutable;
using Cimon.Contracts.CI;
using Cimon.Data.Common;

namespace Cimon.Data.BuildInformation;

public class BuildInfoHistory
{
	public event Action<Item> OnResolved;
	public record BuildConfigurationStats(int Runs, int SuccessfulRunsInARow, int LastSuccessfulBuildAge)
	{
		public bool Resolved { get; set; }
		public bool IsUnstable => LastSuccessfulBuildAge > 5;
	}

	public record Item(BuildInfo Info, bool Resolved, ImmutableList<BuildFailureSuspect> Suspects,
		BuildConfigurationStats Stats)
	{
		public bool Resolved { get; set; } = Resolved;
		public ImmutableList<BuildFailureSuspect> Suspects { get; set; } = Suspects;
		public bool IsLast { get; set; } = true;

		public void SetResolved() {
			Resolved = true;
			Info.FailedTests = ArraySegment<CITestOccurence>.Empty;
			Stats.Resolved = true;
		}
	}

	private readonly RingBuffer<Item> _buffer = new(50);
	private BuildInfo? _actualBuildInfo;

	public BuildInfo? CombinedInfo {
		get {
			if (_actualBuildInfo is null) {
				InitializeLastBuildInfo();
			}
			return _actualBuildInfo;
		}
	}

	public Item Add(BuildInfo newInfo) {
		BuildConfigurationStats stats;
		if (_buffer.Last is { } last) {
			last.IsLast = false;
			var successfulRunsInARow = last.Info.IsSuccess() ? last.Stats.SuccessfulRunsInARow + 1 : 0;
			var lastSuccessfulAge = last.Info.IsOk() ? 1 : last.Stats.LastSuccessfulBuildAge + 1;
			stats = new BuildConfigurationStats(last.Stats.Runs + 1, successfulRunsInARow, lastSuccessfulAge);
		} else {
			stats = new BuildConfigurationStats(0, 0, 100);
		}
		var buildInfoItem = new Item(newInfo, false, ImmutableList<BuildFailureSuspect>.Empty, stats);
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

	public void InitializeLastBuildInfo() {
		_actualBuildInfo = null;
		if (_buffer.Items.Count == 0) return;
		var items = _buffer.ToArray().AsSpan();
		var committers = GetCommittersInfo(items);
		_actualBuildInfo = items[^1].Info with {
			CombinedCommitters = committers
		};
	}

	private IReadOnlyCollection<CommitterInfo> GetCommittersInfo(Span<Item> items) {
		var changes = new List<VcsChange>();
		var suspects = new Dictionary<VcsUser, float>();
		var lastIndex = items.Length - 1;
		for (int i = lastIndex; i >= 0; i--) {
			Item item = items[i];
			if (item.Resolved) break;
			var nextItemIndex = i + 1;
			var isItemResolved = GetIsItemResolved(item, items[nextItemIndex..]);
			if (isItemResolved) {
				item.SetResolved();
				OnResolved?.Invoke(item);
				MarkPreviousItemsAsResolved(items, i);
				break;
			}
			changes.AddRange(item.Info.Changes.Select(c => c with { IsInherited = i != lastIndex }));
			MergeSuspectConfidence(item, suspects);
		}
		suspects = NormalizeSuspects(suspects);
		return changes
			.GroupBy(x => x.Author)
			.Select(g =>
				new CommitterInfo(g.Key, g.Count(), suspects.GetValueOrDefault(g.Key, 0f)))
			.OrderByDescending(x => x.SuspectConfidence)
			.ThenByDescending(x => x.CommitsCount)
			.ToImmutableList();
	}

	private Dictionary<VcsUser, float> NormalizeSuspects(Dictionary<VcsUser, float> suspects) {
		var total = suspects.Sum(x => x.Value);
		if (total < 100) return suspects;
		var weight = 100f / total;
		foreach (var suspect in suspects.ToList()) {
			suspects[suspect.Key] = Convert.ToSingle(Math.Round(suspect.Value * weight));
		}
		return suspects;
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
		string GetTestInfo(CITestOccurence t) => string.IsNullOrWhiteSpace(t.TestId) ? t.Name : t.TestId;
		bool GetIsTestReallyFailing(CITestOccurence x) => x.Ignored is not true && x.CurrentlyMuted is not true;
		var failedTests = item.Info.FailedTests
			.Where(GetIsTestReallyFailing)
			.Select(GetTestInfo)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
		foreach (var nextItem in nextItems) {
			failedTests = failedTests
				.Intersect(nextItem.Info.FailedTests.Where(GetIsTestReallyFailing).Select(GetTestInfo)).ToHashSet();
			if (failedTests.Count == 0) break;
		}
		return unresolvedProblems.Count == 0 && failedTests.Count == 0;
	}
}
