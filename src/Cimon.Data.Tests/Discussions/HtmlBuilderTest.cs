using AngleSharp.Dom;
using Cimon.Data.Discussions;
using Snapshooter.NUnit;

namespace Cimon.Data.Tests.Discussions;

[TestFixture]
[TestOf(typeof(HtmlBuilder))]
public class HtmlBuilderTest
{

	[Test]
	public void AddParagraph_ShouldAddExpectedContent() {
		var builder = HtmlBuilder.Create();
		builder.AddNodeWithText(TagNames.P, "test").ToString()
			.Should().MatchSnapshot();
	}

	[Test]
	public void AddSpoiler() {
		var builder = HtmlBuilder.Create();
		builder.AddSpoiler("test", htmlBuilder => {
			htmlBuilder.AddNodeWithText(TagNames.P, "yo");
		}).ToString()
			.Should().MatchSnapshot();
	}

	[Test]
	public void DoubleNestingAndAttributes() {
		using var builder = HtmlBuilder.Create();
		builder.AddNode(TagNames.P, p => {
				p["a"] = "b";
				p.AddNode(TagNames.Div, div => div.AddNodeWithText(TagNames.P, "test"));
			}).ToString()
			.Should().MatchSnapshot();
	}
}
