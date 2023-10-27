namespace Cimon.Data.ML;

using System.Text;

public static class TextUtils
{
	
	public static string NormalizeText(this string text) {
		var result = new StringBuilder();
		void AddSpace() {
			if (result.Length > 0 && char.IsWhiteSpace(result[^1])) {
				return;
			}
			result.Append(' ');
		}
		for (int i = 0; i < text.Length; i++) {
			var currentChar = text[i];
			if (!char.IsAsciiLetterOrDigit(currentChar)) {
				AddSpace();
				continue;
			}
			result.Append(char.ToLowerInvariant(currentChar));
			if (i >= text.Length - 1)
				continue;
			char nextChar = text[i + 1];
			if (char.IsLower(currentChar) && char.IsUpper(nextChar)) {
				AddSpace();
			}
		}
		return result.ToString();
	}
}