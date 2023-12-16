using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;

namespace Cimon.Shared;

public record struct ReactiveValue<T>
{
	private ReactiveValueHolder<T>? _valueHolder;
	internal ReactiveValueHolder<T>? SourceValueHolder => _valueHolder;
	internal void SetHolder(ReactiveValueHolder<T> holder) => _valueHolder = holder;
	private readonly ReactiveValueHolder<T> ValueHolder => _valueHolder ?? ReactiveValueHolder<T>.Empty;
	public ReactiveValue(T defValue) {
		_valueHolder = new ReactiveValueHolder<T> {
			Source = Observable.Empty<T>(),
			Value = defValue
		};
	}

	public T? Value => ValueHolder.Value;

	[MemberNotNullWhen(true, nameof(Value))]
	public bool HasValue => ValueHolder.Value is not null;

	[MemberNotNullWhen(false, nameof(Value))]
	public bool IsEmpty => _valueHolder is null;
	public static implicit operator T?(ReactiveValue<T> value) => value.ValueHolder.Value ?? default;
	public override string ToString() => _valueHolder?.ToString() ?? string.Empty;

	public ReactiveValue<T> OnChange(Action<T> action) {
		ValueHolder.OnChange(action);
		return this;
	}

	public Task WaitForValueAsync() => ValueHolder.WaitForValueAsync();

	public void Unsubscribe() => ValueHolder.Unsubscribe();

}
