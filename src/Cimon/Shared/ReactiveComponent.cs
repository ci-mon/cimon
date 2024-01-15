namespace Cimon.Shared;

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.AspNetCore.Components;

public class ReactiveComponent : ComponentBase, IDisposable
{
	private readonly List<IDisposable> _disposables = new();
	private volatile bool _initializingReactiveValues; 
	private volatile bool _triggerStateHasChangedAfterInitializeReactiveValues;

	protected ReactiveValue<T> Subscribe<T>(ref ReactiveValue<T> value, IObservable<T> observable, T? def = default) {
		value.Unsubscribe();
		var valueHolder = value.SourceValueHolder;
		if (valueHolder is null) {
			valueHolder = new ReactiveValueHolder<T> {
				Source = observable,
				Value = def ?? value.Value
			};
			value.SetHolder(valueHolder);
		}
		IDisposable subscription = observable.TakeUntil(_disposed).Subscribe(value => {
			_ = InvokeAsync(() => {
				valueHolder.Value = value;
				if (_initializingReactiveValues) {
					_triggerStateHasChangedAfterInitializeReactiveValues = true;
				} else {
					StateHasChanged();
				}
			});
		});
		_disposables.Add(subscription);
		valueHolder.OnUnsubscribe += () => {
			_disposables.Remove(subscription);
			subscription.Dispose();
		};
		return value;
	}

	protected virtual bool PreloadData { get; set; }

	protected override bool ShouldRender() => !_initializingReactiveValues && base.ShouldRender();

	private readonly Subject<bool> _disposed = new();
	[Inject] private IHttpContextAccessor? HttpContextAccessor { get; set; }

	protected virtual void Dispose(bool disposing) {
		_disposed.OnNext(disposing);
		foreach (var disposable in _disposables) {
			disposable.Dispose();
		}
	}

	public void Dispose() => Dispose(true);

	protected virtual Task InitializeReactiveValues() {
		return Task.CompletedTask;
	}

	protected override async Task OnParametersSetAsync() {
		await base.OnParametersSetAsync();
		try {
			_initializingReactiveValues = true;
			if (HttpContextAccessor?.HttpContext?.Response.HasStarted != false || PreloadData) {
				await InitializeReactiveValues();
			}
		}
		finally {
			_initializingReactiveValues = false;
		}
		if (_triggerStateHasChangedAfterInitializeReactiveValues) {
			_triggerStateHasChangedAfterInitializeReactiveValues = false;
			StateHasChanged();
		}
	}
}
