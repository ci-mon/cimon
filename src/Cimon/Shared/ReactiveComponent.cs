using System.Diagnostics.CodeAnalysis;

namespace Cimon.Shared;

using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using Microsoft.AspNetCore.Components;

public record ReactiveValue<T>
{
	public required IObservable<T> Source { get; init; }

	public required T? Value {
		get => _value;
		set {
			if (Equals(value, _value)) {
				return;
			}
			_value = value;
			foreach (var handler in _handlers) {
				handler(value);
			}
		}
	}

	[MemberNotNullWhen(true, nameof(Value))]
	public bool HasValue => _value is not null;
	[MemberNotNullWhen(false, nameof(Value))]
	public bool IsEmpty => _value is null;

	public override string ToString() => Value?.ToString() ?? string.Empty;

	private readonly List<Action<T>> _handlers = new();
	private T? _value;

	public ReactiveValue<T> OnChange(Action<T> action) {
		_handlers.Add(action);
		if (HasValue) {
			action(Value);
		}
		return this;
	}

	public static implicit operator T?(ReactiveValue<T> value) => value.Value;
}

public class ReactiveComponent : ComponentBase, IDisposable
{

	private readonly List<IDisposable> _disposables = new();
	protected ReactiveValue<T> Subscribe<T>(IObservable<T> observable, T initial) {
		var result = new ReactiveValue<T> {
			Value = initial,
			Source = observable
		};
		_disposables.Add(observable.TakeUntil(_disposed).Subscribe(value => {
			_ = InvokeAsync(() => {
				result.Value = value;
				StateHasChanged();
			});
		}));
		return result;
	}

	protected ReactiveValue<T> Subscribe<T>(IObservable<T> observable) => Subscribe(observable, default)!;

	private readonly Subject<bool> _disposed = new();
	protected virtual void Dispose(bool disposing) {
		_disposed.OnNext(disposing);
		foreach (var disposable in _disposables) {
			disposable.Dispose();
		}
	}

	public void Dispose() => Dispose(true);
}
