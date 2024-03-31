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
	IBuildInfoProvider buildInfoProvider,
	BuildInfo BuildInfo,
	IActorRef Receiver);

record MlResponse(MlRequest Request, BuildFailureSuspect Suspect);


class BuildMLActor: ReceiveActor
{
	private readonly ILogger<BuildMLActor> _logger;
	private CancellationTokenSource? _cts;
	private ImmutableQueue<MlRequest> _requests = ImmutableQueue<MlRequest>.Empty;
	private MlRequest _active;
	private IServiceScope _serviceScope;

	private record LogsResult(string Log, MlRequest Request);
	public BuildMLActor(IServiceProvider serviceProvider, ILogger<BuildMLActor> logger) {
		_serviceScope = serviceProvider.CreateAsyncScope();
		_logger = logger;
		Receive<MlRequest>(HandleRequest);
	}

	private void HandleRequest(MlRequest request) {
		EnqueueRequest(request);
		ProcessNextItem();
	}

	private void ProcessNextItem() {
		if (_requests.IsEmpty) {
			return;
		}
		_requests = _requests.Dequeue(out var next);
		GetLogs(next);
		BecomeStacked(DownloadingLogs);
	}

	private void GetLogs(MlRequest request) {
		_active = request;
		_cts?.Dispose();
		_cts = new CancellationTokenSource();
		var query = new LogsQuery(request.ConnectorInfo, request.BuildConfig, request.BuildInfo, _cts.Token);
		request.buildInfoProvider.GetLogs(query).PipeTo(Self, Self, msg => new LogsResult(msg, request), 
			e => new LogsResult(e.Message, request));
	}

	private void DownloadingLogs() {
		Receive<MlRequest>(request => {
			if (_active?.BuildConfig.Id == request.BuildConfig.Id) {
				_cts?.Cancel();
				UnbecomeStacked();
				Context.Self.Forward(request);
				return;
			}
			EnqueueRequest(request);
		});
		ReceiveAsync<LogsResult>(async logs => {
			UnbecomeStacked();
			ProcessNextItem();
			var request = logs.Request;
			var info = request.BuildInfo;
			info.Log = logs.Log;
			BuildFailureSuspect? failureSuspect = null;
			try {
				var predictor = _serviceScope.ServiceProvider.GetRequiredService<IBuildFailurePredictor>();
				failureSuspect = await predictor.FindFailureSuspect(info);
			} catch (Exception e) {
				_logger.LogWarning("Failed to find suspect on {BuildName} {BuildId}. Error: {Error}",
					info.Name, info.Id, e.Message);
			}
			info.Log = info.Log?.Substring(0, Math.Min(10000, info.Log.Length));
			if (failureSuspect is not null && failureSuspect.Confidence > 20) {
				request.Receiver.Tell(new MlResponse(request, failureSuspect));
			}
		});
	}

	private void EnqueueRequest(MlRequest request) {
		_requests = _requests.Enqueue(request);
	}

	public override void AroundPostStop() {
		_cts?.Cancel();
		_serviceScope.Dispose();
		base.AroundPostStop();
	}
}
