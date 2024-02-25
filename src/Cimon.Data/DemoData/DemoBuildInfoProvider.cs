using Cimon.Contracts.CI;
using Cimon.Contracts.Services;
using Cimon.DB.Models;

namespace Cimon.Data.DemoData;

public class DemoBuildInfoProvider : IBuildInfoProvider
{
	public Task<IReadOnlyCollection<BuildInfo>> FindInfo(BuildInfoQuery infoQuery) {
		IReadOnlyCollection<BuildInfo> result = Array.Empty<BuildInfo>();
		if (infoQuery.BuildConfig is BuildConfigModel { DemoState: {} demoState }) {
			var status = demoState.StatusText?.Contains("(not stable)") is true
				? Enum.GetValues<BuildStatus>()[Random.Shared.Next(3)]
				: demoState.Status;
			var newState = demoState with {
				Id = int.TryParse(demoState.Id, out var num) ? $"{num+1}" : 0.ToString(),
				Duration = TimeSpan.FromMinutes(Random.Shared.Next(120)),
				StartDate = DateTimeOffset.UtcNow.AddMinutes(-1 * Random.Shared.Next(120)),
				Status = status
			};
			result = new List<BuildInfo> { newState };
		}
		return Task.FromResult(result);
	}

	public Task<string> GetLogs(LogsQuery logsQuery) {
		return Task.FromResult($"some logs from build {logsQuery.BuildInfo.Name}");
	}
}
