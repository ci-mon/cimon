using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;

namespace Cimon.Data.BuildInformation;

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

record MlResponse(MlRequest Request, ImmutableList<BuildFailureSuspect> Suspects);

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
		var failureSuspect = await TryFindSuspects(request.BuildInfo, false);
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
			var failureSuspect = await TryFindSuspects(info, true);
			info.Log = info.Log?.Substring(0, Math.Min(10000, info.Log.Length));
			TryPublishSuspect(failureSuspect, request);
			UnbecomeStacked();
			Stash.UnstashAll();
		});
	}

	private async Task<ImmutableList<BuildFailureSuspect>?> TryFindSuspects(BuildInfo info, bool useLogs) {
		try {
			_logger.LogInformation("Finding suspects for {Build}, Logs={Logs}", info.Name, useLogs);
			var predictor = _serviceScope.ServiceProvider.GetRequiredService<IBuildFailurePredictor>();
			var failureSuspects = await predictor.FindFailureSuspects(info, useLogs);
			return failureSuspects;
		} catch (Exception e) {
			_logger.LogWarning("Failed to find suspect on {BuildName} {BuildId}. Error: {Error}",
				info.Name, info.Id, e.Message);
		}
		return null;
	}

	private static bool TryPublishSuspect(ImmutableList<BuildFailureSuspect>? failureSuspects, MlRequest request) {
		if (failureSuspects is null) return false;
		request.Receiver.Tell(new MlResponse(request, failureSuspects));
		// todo mark failed test as not needed here or in build info history
		return true;
	}

	public override void AroundPostStop() {
		_cts?.Cancel();
		_serviceScope.Dispose();
		base.AroundPostStop();
	}

	public IStash Stash { get; set; } = null!;
}
