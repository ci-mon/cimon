using System.Runtime.CompilerServices;

namespace Cimon.Shared;

using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using Microsoft.AspNetCore.Components;

public static class ReactiveComponentUtils
{
	public static IObservable<T> Subscribe<T>(this IObservable<T> source, ReactiveComponent component, 
			Expression<Func<T?>> fieldExpression, Action<T>? callback = null) {
		component.Subscribe(source, fieldExpression, callback);
		return source;
	}
}

public class ReactiveComponent : ComponentBase, IDisposable
{

	private readonly List<IDisposable> _disposables = new();
	internal void Subscribe<T>(IObservable<T> source, Expression<Func<T?>> fieldExpression, Action<T>? callback = null) {
		if (fieldExpression.Body is not MemberExpression { Member: FieldInfo field }) {
			throw new InvalidOperationException();
		}
		_disposables.Add(source.TakeUntil(_disposed).Subscribe(value => {
			InvokeAsync(() => {
				field.SetValue(this, value);
				callback?.Invoke(value);
				StateHasChanged();
			});
		}));
	}

	private readonly Subject<bool> _disposed = new();
	protected virtual void Dispose(bool disposing) {
		_disposed.OnNext(disposing);
	}

	public void Dispose() => Dispose(true);
}
