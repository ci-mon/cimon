using Cimon.Data.ML;

namespace Cimon.Data.Tests;

[TestFixture]
[TestOf(typeof(TextCompressor))]
public class TextCompressorTest
{

	[Test]
	public void CompressText() {
		var text = "tests failed 11 passed passed 6900 failed muted failed ignored 2 muted muted failed tests 11";
		var result = TextCompressor.CompressText(text, 72);
		result.Should().Be("tests failed 11 passed 6900 failed muted ignored 2 muted");
	}
}
