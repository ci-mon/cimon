using NUnit.Framework;

namespace Cimon.Data.Tests;

using System.Diagnostics;

public static class TestUtils
{
	public static Task WaitForAssert(Action func) {
		return WaitFor(() => {
			try {
				func();
				return true;
			} catch (Exception e) {
				return false;
			}
		});
	}

	public static async Task WaitFor(Func<bool> func) {
		try {
			using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(50));
			var cts = new CancellationTokenSource();
			cts.CancelAfter(TimeSpan.FromSeconds(Debugger.IsAttached ? 30 : 5));
			while (await timer.WaitForNextTickAsync(cts.Token)) {
				if (func()) {
					return;
				}
				cts.Token.ThrowIfCancellationRequested();
			}
		} catch (OperationCanceledException) {
			Assert.Fail("Wait timeout");
		}
	}
}
