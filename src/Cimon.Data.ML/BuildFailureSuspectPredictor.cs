namespace Cimon.Data.ML;

using System.Text;
using Cimon.Contracts.CI;
using Microsoft.ML;
using Microsoft.ML.Transforms.Text;

public interface IBuildFailurePredictor
{
	BuildFailureSuspect? FindFailureSuspect(BuildInfo buildInfo);
}

public class BuildFailurePredictor : IBuildFailurePredictor
{
	public BuildFailureSuspect? FindFailureSuspect(BuildInfo buildInfo) {
		var textData = ExtractTextData(buildInfo);
		TextData buildStatusTextData = new TextData { Text = textData.BuildStatus.NormalizeText() };
		var changesTextData = textData.Changes.Select(x => new TextData {
			Text = x.Item2.NormalizeText()
		}).ToList();
		try {
			(int index, float confidence) = FindBestMatch(buildStatusTextData, changesTextData);
			if (index == -1) {
				return null;
			}
			(VcsUser, string) item = textData.Changes[index];
			return new BuildFailureSuspect(item.Item1, confidence);
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

	private static (int Index, int Confidence) FindBestMatch(TextData source, IList<TextData> itemsToMatch) {
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
		var confidence = bestFit.Value / probabilities.Sum() * 100;
		return (bestFit.Index, Convert.ToInt32(confidence));
	}


	private static void PrintLdaFeatures(TransformedTextData prediction) {
		for (int i = 0; i < prediction.Features.Length; i++)
			Console.Write($"{prediction.Features[i]:F4}  ");
		Console.WriteLine();
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
