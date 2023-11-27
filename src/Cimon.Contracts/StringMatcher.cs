using System.Text.RegularExpressions;

namespace Cimon.Contracts;

public class StringMatcher
{
	private readonly bool _matchAny;
	private readonly List<Regex> _matchers = new();
	public static StringMatcher AnyString { get; } = new(null);

	public StringMatcher(string? rules, string ruleSeparator = ";") {
		if (string.IsNullOrWhiteSpace(rules)) {
			_matchAny = true;
		} else {
			var ruleItems = rules.Split(ruleSeparator, StringSplitOptions.RemoveEmptyEntries);
			foreach (var ruleItem in ruleItems) {
				_matchers.Add(new Regex(ruleItem.Replace("*", "(.*?)")));
			}
		}
	}

	public bool Check(string? value) {
		return _matchAny || (value is not null && _matchers.Any(m=>m.IsMatch(value)));
	}
}
