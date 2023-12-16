using System.Reactive.Linq;

namespace Cimon.Shared;

internal class ReactiveValueHolder<T>
{
	public static ReactiveValueHolder<T> Empty { get; } = new() {
		Source = Observable.Empty<T>(),
		Value = default
	};

	private readonly TaskCompletionSource _initialized = new();
	public required IObservable<T> Source { get; init; }

	public required T? Value {
		get => _value;
		set {
			_initialized.TrySetResult();
			if (Equals(value, _value)) {
				return;
			}
			_value = value;
			foreach (var handler in _handlers) {
				handler(value);
			}
		}
	}
	public event Action? OnUnsubscribe;
	public override string ToString() => Value?.ToString() ?? string.Empty;

	private readonly List<Action<T>> _handlers = new();
	private T? _value;


	public void OnChange(Action<T> action) {
		_handlers.Add(action);
		if (Value is {} value) {
			action(value);
		}
	}

	public Task WaitForValueAsync() => _initialized.Task;

	public void Unsubscribe() {
		OnUnsubscribe?.Invoke();
		OnUnsubscribe = null;
	}
}
