using NUnit.Framework;

namespace Cimon.Data.Tests;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using FluentAssertions.Execution;

public static class Wait
{
	public static Task ForAssert(Action func, 
			[CallerArgumentExpression("func")] string expression = null) {
		return For(() => {
			func();
			return true;
		}, true, expression);
	}

	private static async Task For(Func<bool> func, bool catchExceptions = false,
			[CallerArgumentExpression("func")] string expression = null) {
		Exception lastException = null;
		try {
			using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(10));
			var cts = new CancellationTokenSource();
			cts.CancelAfter(TimeSpan.FromSeconds(Debugger.IsAttached ? 30 : 5));
			while (await timer.WaitForNextTickAsync(cts.Token)) {
				try {
					using (new AssertionScope()) {
						if (func()) {
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
}
