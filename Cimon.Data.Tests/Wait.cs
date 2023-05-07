namespace Cimon.Data.Tests;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using FluentAssertions.Execution;

public static class Wait
{
	public static Task ForAssert(Action func,
		[CallerArgumentExpression("func")] string expression = null) {
		return ForAssert(() => {
			func();
			return Task.CompletedTask;
		}, expression);
	}

	public static Task ForAssert(Func<Task> func, 
			[CallerArgumentExpression("func")] string expression = null) {
		return For(async () => {
			await func();
			return true;
		}, true, expression);
	}

	private static async Task For(Func<Task<bool>> func, bool catchExceptions = false,
			[CallerArgumentExpression("func")] string expression = null) {
		Exception? lastException = null;
		try {
			using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(10));
			var cts = new CancellationTokenSource();
			cts.CancelAfter(TimeSpan.FromSeconds(Debugger.IsAttached ? 30 : 5));
			while (await timer.WaitForNextTickAsync(cts.Token)) {
				try {
					using (new AssertionScope()) {
						if (await func()) {
							return;
						}
					}
				} catch (Exception e) when(catchExceptions) {
					lastException = e;
				}
				cts.Token.ThrowIfCancellationRequested();
			}
		} catch (OperationCanceledException) {
			Assert.Fail($"Wait timeout: {expression}.{Environment.NewLine}{lastException}");
		}
	}

	public static Task ForConditionNotChanged(Action func, 
		[CallerArgumentExpression("func")] string expression = null) {
		return ForConditionNotChanged(() => {
			func();
			return true;
		}, true, expression);
	}

	private static async Task ForConditionNotChanged(Func<bool> func, bool catchExceptions = false,
		[CallerArgumentExpression("func")] string expression = null) {
		Exception? lastException = null;
		try {
			using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(10));
			var cts = new CancellationTokenSource();
			cts.CancelAfter(TimeSpan.FromSeconds(5));
			while (await timer.WaitForNextTickAsync(cts.Token)) {
				try {
					using (new AssertionScope()) {
						if (!func()) {
							Assert.Fail($"Condition not met: {expression}.{Environment.NewLine}{lastException}");
						}
					}
				} catch (Exception e) when(catchExceptions) {
					lastException = e;
				}
				cts.Token.ThrowIfCancellationRequested();
			}
		} catch (OperationCanceledException) { }
	}
}
