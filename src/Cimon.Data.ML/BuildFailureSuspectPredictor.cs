namespace Cimon.Data.ML;

using System.Text;
using Contracts.CI;
using Microsoft.ML;
using Microsoft.ML.Transforms.Text;

public interface IBuildFailurePredictor
{
	BuildFailureSuspect? FindFailureSuspect(BuildInfo buildInfo);
}

public class BuildFailurePredictor : IBuildFailurePredictor
{
	readonly record struct BestMatch(int Index, int Confidence)
	{
		public static BestMatch Empty { get; } = new(-1, 0);
		public bool IsEmpty() => Index == Empty.Index;
	}

	public BuildFailureSuspect? FindFailureSuspect(BuildInfo buildInfo) {
		var textData = ExtractTextData(buildInfo);
		TextData buildStatusTextData = new TextData { Text = textData.BuildStatus.NormalizeText() };
		var changesTextData = textData.Changes.Select(x => new TextData {
			Text = x.Item2.NormalizeText()
		}).Where(x=>!string.IsNullOrWhiteSpace(x.Text)).ToList();
		if (!changesTextData.Any()) {
			return null;
		}
		try {
			var match = FindBestMatch(buildStatusTextData, changesTextData);
			if (match.IsEmpty()) {
				return null;
			}
			(VcsUser, string) item = textData.Changes[match.Index];
			return new BuildFailureSuspect(item.Item1, match.Confidence);
		} catch (Exception e) {
			Console.WriteLine(e);
			return null;
		}
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
			return (x.Key, changesText.ToString());
		});
		return new BuildInfoTextData(status, changes.ToList());
	}

	private static BestMatch FindBestMatch(TextData source, IList<TextData> itemsToMatch) {
		var mlContext = new MLContext();
		var dataView = mlContext.Data.LoadFromEnumerable(itemsToMatch.Prepend(source));
		var pipeline = mlContext.Transforms.Text
			.NormalizeText("NormalizedText", "Text", keepDiacritics: false,
				keepPunctuations: false, keepNumbers: false)
			.Append(mlContext.Transforms.Text.TokenizeIntoWords("Words", "NormalizedText"))
			.Append(mlContext.Transforms.Text.RemoveDefaultStopWords("Tokens", "Words",
				language: StopWordsRemovingEstimator.Language.English))
			.Append(mlContext.Transforms.Conversion.MapValueToKey("Tokens"))
			.Append(mlContext.Transforms.Text.ProduceNgrams("Ngrams", "Tokens", 
				ngramLength: 3, useAllLengths: true, weighting: NgramExtractingEstimator.WeightingCriteria.Tf))
			.Append(mlContext.Transforms.Text.LatentDirichletAllocation("Features", "Ngrams", 
				numberOfTopics: itemsToMatch.Count + 1));
		var transformer = pipeline.Fit(dataView);
		var predictionEngine = mlContext.Model.CreatePredictionEngine<TextData, TransformedTextData>(transformer);
		TransformedTextData? sourceRes = predictionEngine.Predict(source);
		var sourceTopic = sourceRes.Features.GetItemWithMaxValue();
		var probabilities = itemsToMatch.Select(item => {
			var prediction = predictionEngine.Predict(item);
			var sourceTopicWeight = prediction.Features[sourceTopic.Index];
			return sourceTopicWeight;
		}).ToList();
		var bestFit = probabilities.GetItemWithMaxValue();
		var totalProbabilities = probabilities.Sum();
		if (totalProbabilities == 0) {
			return BestMatch.Empty;
		}
		var confidence = bestFit.Value / totalProbabilities * 100;
		return new BestMatch(bestFit.Index, Convert.ToInt32(confidence));
	}

	private class TextData
	{
		public string Text { get; set; }
	}

	private class TransformedTextData : TextData
	{
		public float[] Features { get; set; }
	}
}
