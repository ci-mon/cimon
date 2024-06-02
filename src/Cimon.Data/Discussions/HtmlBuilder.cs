using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

namespace Cimon.Data.Discussions;

public sealed class HtmlBuilder : IDisposable
{
	private readonly Context _context;
	private readonly IElement _body;
	private readonly bool _skipDispose;

	private sealed class Context(IHtmlDocument document) : IDisposable
	{
		public IHtmlDocument Document { get; } = document;

		public HtmlBuilder AddNode(string name, INode parent) {
			var node = Document.CreateElement(name);
			parent.AppendChild(node);
			return new HtmlBuilder(this, node, true);
		}
		public void Dispose() => Document.Dispose();
	}
	public static HtmlBuilder Create() {
		var parser = new HtmlParser();
		var document = parser.ParseDocument("<html><body></body></html>");
		var context = new Context(document);
		return new HtmlBuilder(context, document.Body!);
	}
	private HtmlBuilder(Context context, IElement body, bool skipDispose = false) {
		_context = context;
		_body = body;
		_skipDispose = skipDispose;
	}

	public HtmlBuilder AddNodeWithText(string name, string content) =>
		AddNode(name, x => x.AddText(content));

	public HtmlBuilder AddText(string content) {
		_body.AppendChild(_context.Document.CreateTextNode(content));
		return this;
	}

	public HtmlBuilder AddSpoiler(string header, Action<HtmlBuilder> content) {
		return AddNode("details", builder => {
			builder.AddNodeWithText("summary", header)
				.AddNode(TagNames.Div, content);
		});
	}

	public HtmlBuilder AddNode(string name, Action<HtmlBuilder> content) {
		var ctx = _context.AddNode(name, _body);
		content(ctx);
		return this;
	}

	public override string ToString() => _body.InnerHtml;

	public void Dispose() {
		if (!_skipDispose)
			_context.Dispose();
	}

	public string? this[string attr] {
		get => _body.GetAttribute(attr);
		set => _body.SetAttribute(attr, value);
	}
}
