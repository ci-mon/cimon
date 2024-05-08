using Cimon.Contracts.CI;
using Cimon.Contracts.Services;
using Cimon.DB.Models;

namespace Cimon.Data.DemoData;

public class DemoBuildInfoProvider : IBuildInfoProvider
{
	private long _callsCount;
	public Task<IReadOnlyList<BuildInfo>> FindInfo(BuildInfoQuery infoQuery) {
		IReadOnlyList<BuildInfo> result = Array.Empty<BuildInfo>();
		if (infoQuery.BuildConfig is BuildConfigModel { DemoState: {} demoState }) {
			Interlocked.Increment(ref _callsCount);
			var status = demoState.StatusText?.Contains("(not stable)") is true
				? Enum.GetValues<BuildStatus>()[Random.Shared.Next(3)]
				: demoState.Status;
			if (_stateForAll is {} state) {
				if (demoState.IsNotOk() != state) {
					return Task.FromResult<IReadOnlyList<BuildInfo>>(new[] { demoState });
				}
				status = state ? BuildStatus.Success : BuildStatus.Failed;
			}
			var newState = demoState with {
				Id = int.TryParse(demoState.Id, out var number) ? $"{number + _callsCount}" : _callsCount.ToString(),
				Duration = TimeSpan.FromMinutes(Random.Shared.Next(120)),
				StartDate = DateTimeOffset.UtcNow.AddMinutes(-1 * Random.Shared.Next(120)),
				Status = status
			};
			if (DemoBuildInfos.TryGetValue(infoQuery.BuildConfig.Key, out var demo)) {
				newState.Status = demo.Status;
				newState.Id = demo.Id;
				newState.StatusText = demo.StatusText;
				newState.Problems = demo.Problems;
				newState.Changes = demo.Changes;
				newState.FailedTests = demo.FailedTests;
			}
			result = new List<BuildInfo> { newState };
		}
		return Task.FromResult(result);
	}

	public Task<string> GetLogs(LogsQuery logsQuery) => 
		Task.FromResult($"some logs from build {logsQuery.BuildInfo.Name}");

	private static bool? _stateForAll;
	public static void SetStateForAll(bool? value) => _stateForAll = value;

	private static readonly Dictionary<string, DemoBuildInfo> DemoBuildInfos = new();
	public static void SetBuildState(string buildConfigKey, DemoBuildInfo demoBuildInfo) =>
		DemoBuildInfos[buildConfigKey] = demoBuildInfo;
}

public record DemoBuildInfo(
	BuildStatus Status,
	string Id,
	string StatusText,
	IReadOnlyCollection<CIBuildProblem> Problems,
	IReadOnlyCollection<CITestOccurence> FailedTests,
	IReadOnlyCollection<VcsChange> Changes);
