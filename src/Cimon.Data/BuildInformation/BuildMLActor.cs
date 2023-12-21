namespace Cimon.Data.BuildInformation;

using Akka.Actor;
using Cimon.Contracts.CI;
using Cimon.Contracts.Services;
using Cimon.Data.ML;
using Microsoft.Extensions.Logging;

class BuildMLActor: ReceiveActor
{
	private readonly CancellationTokenSource _cts;

	private record LogsResult(string Log, BuildInfo BuildInfo);
	public BuildMLActor(CIConnectorInfo connectorInfo, BuildConfig buildConfig, IBuildInfoProvider buildInfoProvider, 
			IBuildFailurePredictor buildFailurePredictor, ILogger<BuildMLActor> logger) {
		_cts = new CancellationTokenSource();
		Receive<BuildInfo>(info => {
			var query = new LogsQuery(connectorInfo, buildConfig, info, _cts.Token);
			buildInfoProvider.GetLogs(query).PipeTo(Self, Self,
				msg => new LogsResult(msg, info), e => new LogsResult(e.Message, info));
		});
		Receive<LogsResult>(logs => {
			var info = logs.BuildInfo;
			info.Log = logs.Log;
			BuildFailureSuspect? failureSuspect = null;
			try {
				failureSuspect = buildFailurePredictor.FindFailureSuspect(info);
			} catch (Exception e) {
				logger.LogWarning("Failed to find suspect on {BuildName} {BuildId}. Error: {Error}",
					info.Name, info.Id, e.Message);
			}
			info.Log = info.Log?.Substring(0, Math.Min(10000, info.Log.Length));
			if (failureSuspect is not null) {
				Context.Parent.Tell(failureSuspect);
			}
		});
	}

	public override void AroundPostStop() {
		_cts.Cancel();
		base.AroundPostStop();
	}
}
