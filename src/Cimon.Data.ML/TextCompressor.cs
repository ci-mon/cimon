using System.Text;

namespace Cimon.Data.ML;

public class TextCompressor
{
	private record WordStats(string Word, List<WordPosition> Positions);

	private record WordPosition(int Number, WordStats Stats)
	{
		public WordPosition? Next { get; set; }
	}

	public static string CompressText(string sourceText, int maxLength) {
		var firstWord = BuildTextStructure(sourceText, out var maxRepetitions);
		if (TryCompressText(maxRepetitions, firstWord, maxLength) is { } compressed) {
			return compressed;
		}
		return sourceText.Substring(maxLength);
	}

	private static string? TryCompressText(int maxRepetitions, WordPosition? firstWord, int maxLength) {
		var resultBuilder = new StringBuilder(maxLength);
		var maxTries = Math.Max(Math.Round(Math.Log2(maxRepetitions), MidpointRounding.AwayFromZero), 5);
		var skipIndex = maxRepetitions / 2d;
		var result = false;
		var maxSkipIndex = maxRepetitions *1d;
		var minSkipIndex = 0d;
		while (--maxTries >= 0 && Math.Abs(maxSkipIndex - minSkipIndex) * 1d / maxSkipIndex > 0.1) {
			result = Build(maxRepetitions, firstWord, maxLength, skipIndex, resultBuilder);
			if (result) {
				maxSkipIndex = skipIndex;
				skipIndex -= (skipIndex - minSkipIndex) / 2;
			} else {
				minSkipIndex = skipIndex;
				skipIndex += (maxSkipIndex - skipIndex) / 2;
			}
		}
		if (!result) {
			result = Build(maxRepetitions, firstWord, maxLength, maxSkipIndex, resultBuilder);
		}
		return result ? resultBuilder.ToString() : null;
	}

	private static bool Build(int maxRepetitions, WordPosition? firstWord, int maxLength, double skipIndex,
		StringBuilder result) {
		var percentage = skipIndex / maxRepetitions;
		result.Clear();
		var current = firstWord;
		while (current is not null) {
			var count = current!.Stats.Positions.Count;
			var toPreserve = count - (count * percentage);
			if (current.Number <= toPreserve) {
				if (result.Length > 0) {
					result.Append(' ');
				}
				result.Append(current.Stats.Word);
				if (result.Length > maxLength) {
					return false;
				}
			}
			current = current.Next;
		}
		return result.Length <= maxLength;
	}

	private static WordPosition? BuildTextStructure(string text, out int maxRepetitions) {
		var structure = new Dictionary<string, WordStats>();
		WordPosition lastWord = null;
		WordPosition firstWord = null;
		maxRepetitions = 0;
		for (int i = 0; i < text.Length; i++) {
			if (char.IsWhiteSpace(text[i])) continue;
			var wordStart = i;
			while (i < text.Length && !char.IsWhiteSpace(text[i])) {
				i++;
			}
			var wordRange = new Range(wordStart, i);
			var word = text[wordRange];
			if (!structure.TryGetValue(word, out var stats)) {
				stats = new WordStats(word, []);
				structure[word] = stats;
			}
			var wordPosition = new WordPosition(stats.Positions.Count, stats);
			stats.Positions.Add(wordPosition);
			if (maxRepetitions < stats.Positions.Count) {
				maxRepetitions = stats.Positions.Count;
			}
			if (lastWord is not null) {
				lastWord.Next = wordPosition;
			} else {
				firstWord = wordPosition;
			}
			lastWord = wordPosition;
		}
		return firstWord;
	}
}
