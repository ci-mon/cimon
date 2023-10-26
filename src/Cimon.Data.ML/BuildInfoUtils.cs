namespace Cimon.Data.ML;

using System.Text;
using Cimon.Contracts;
using Cimon.Contracts.CI;

public record BuildInfoTextData(string BuildStatus, IReadOnlyCollection<(UserName, string)> Changes);

public class BuildInfoUtils
{
	public UserName? FindProbableFailureAuthor(BuildInfo buildInfo) {
		return null;
	}

	public static BuildInfoTextData ExtractTextData(BuildInfo buildInfo) {
		var problems = string.Join(Environment.NewLine,
			buildInfo.Problems.Select(x => $"{x.ShortSummary} {x.Details}"));
		var testFailures = string.Join(Environment.NewLine,
			buildInfo.FailedTests.Select(t => $"{t.Name}{Environment.NewLine}{t.Details}"));
		var status = $"{buildInfo.StatusText}{Environment.NewLine}{problems}{Environment.NewLine}{testFailures}";
		var changes = buildInfo.Changes.GroupBy(x => x.Author).Select(x => {
			var changesText = new StringBuilder();
			foreach (VcsChange change in x) {
				changesText.AppendLine(change.CommitMessage);
				foreach (var modification in change.Modifications) {
					changesText.AppendLine(modification.Path);
				}
			}
			return (x.Key.Name, changesText.ToString());
		});
		return new BuildInfoTextData(status, changes.ToList());
	}
}
