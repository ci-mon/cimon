using Microsoft.Extensions.DependencyInjection;

namespace Cimon.Data.BuildInformation;

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Akka.Actor;
using Cimon.Contracts.CI;
using Cimon.Contracts.Services;
using Cimon.Data.ML;
using Microsoft.Extensions.Logging;

record MlRequest(
	CIConnectorInfo ConnectorInfo,
	BuildConfig BuildConfig,
	IBuildInfoProvider BuildInfoProvider,
	BuildInfo BuildInfo,
	IActorRef Receiver);

record MlResponse(MlRequest Request, BuildFailureSuspect Suspect);

class BuildMLActor: ReceiveActor, IWithUnboundedStash
{
	private readonly ILogger<BuildMLActor> _logger;
	private CancellationTokenSource? _cts;
	private readonly IServiceScope _serviceScope;

	private record LogsResult(string Log, MlRequest Request);
	public BuildMLActor(IServiceProvider serviceProvider, ILogger<BuildMLActor> logger) {
		_serviceScope = serviceProvider.CreateAsyncScope();
		_logger = logger;
		ReceiveAsync<MlRequest>(HandleRequest);
	}

	private async Task HandleRequest(MlRequest request) {
		var failureSuspect = await TryFindSuspect(request.BuildInfo, false);
		if (!TryPublishSuspect(failureSuspect, request)) {
			GetLogs(request);
			BecomeStacked(DownloadingLogs);
		}
	}

	private void GetLogs(MlRequest request) {
		_cts?.Dispose();
		_cts = new CancellationTokenSource();
		var query = new LogsQuery(request.ConnectorInfo, request.BuildConfig, request.BuildInfo, _cts.Token);
		request.BuildInfoProvider.GetLogs(query).PipeTo(Self, Self, msg => new LogsResult(msg, request), 
			e => new LogsResult(e.Message, request));
	}

	private void DownloadingLogs() {
		Receive<MlRequest>(x => Stash.Stash());
		ReceiveAsync<LogsResult>(async logs => {
			var request = logs.Request;
			var info = request.BuildInfo;
			info.Log = logs.Log;
			var failureSuspect = await TryFindSuspect(info, true);
			info.Log = info.Log?.Substring(0, Math.Min(10000, info.Log.Length));
			TryPublishSuspect(failureSuspect, request);
			UnbecomeStacked();
			Stash.UnstashAll();
		});
	}

	private async Task<BuildFailureSuspect?> TryFindSuspect(BuildInfo info, bool useLogs) {
		try {
			var predictor = _serviceScope.ServiceProvider.GetRequiredService<IBuildFailurePredictor>();
			var failureSuspect = await predictor.FindFailureSuspect(info, useLogs);
			return failureSuspect;
		} catch (Exception e) {
			_logger.LogWarning("Failed to find suspect on {BuildName} {BuildId}. Error: {Error}",
				info.Name, info.Id, e.Message);
		}
		return null;
	}

	private static bool TryPublishSuspect(BuildFailureSuspect? failureSuspect, MlRequest request) {
		if (failureSuspect?.Confidence > 20) {
			request.Receiver.Tell(new MlResponse(request, failureSuspect));
			return true;
		}
		return false;
	}

	public override void AroundPostStop() {
		_cts?.Cancel();
		_serviceScope.Dispose();
		base.AroundPostStop();
	}

	public IStash Stash { get; set; }
}
