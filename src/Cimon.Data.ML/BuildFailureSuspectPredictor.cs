using System.Collections.Immutable;
using System.Collections.Specialized;
using Cimon.Contracts.AppFeatures;
using Microsoft.FeatureManagement;
using SmartComponents.LocalEmbeddings;

namespace Cimon.Data.ML;

using System.Text;
using Contracts.CI;
using Microsoft.ML;
using Microsoft.ML.Transforms.Text;

public interface IBuildFailurePredictor
{
	Task<ImmutableList<BuildFailureSuspect>?> FindFailureSuspects(BuildInfo buildInfo, bool useLog);
}
public class BuildFailurePredictor(IFeatureManager featureManager) : IBuildFailurePredictor
{
	readonly record struct BestMatch(int Index, int ConfidencePercent)
	{
		public static BestMatch Empty { get; } = new(-1, 0);
		public bool IsEmpty() => Index == Empty.Index;
	}

	public async Task<ImmutableList<BuildFailureSuspect>?> FindFailureSuspects(BuildInfo buildInfo, bool useLog) {
		var textData = ExtractTextData(buildInfo, useLog);
		TextData buildStatusTextData = new TextData { Text = textData.BuildStatus.NormalizeText() };
		var changesTextData = textData.Changes.Select(x => new TextData {
			Text = x.Item2.NormalizeText()
		}).Where(x=>!string.IsNullOrWhiteSpace(x.Text)).ToList();
		if (!changesTextData.Any()) {
			return null;
		}
		var useSmartComponents = await featureManager.IsEnabled<MlFeatures.UseSmartComponentsToFindFailureSuspect>();
		BestMatch match = useSmartComponents
			? FindBestMatchWithSmartComponents(buildStatusTextData, changesTextData)
			: FindBestMatchByTfNgrams(buildStatusTextData, changesTextData);
		if (match.IsEmpty()) {
			return null;
		}
		(VcsUser, string) item = textData.Changes[match.Index];
		var failureSuspect = new BuildFailureSuspect(item.Item1, match.ConfidencePercent);
		return ImmutableList.Create(new[] { failureSuspect });
	}

	private BestMatch FindBestMatchWithSmartComponents(TextData buildStatusTextData, List<TextData> changesTextData) {
		using var embedder = new LocalEmbedder();
		var buildStatus = embedder.Embed(buildStatusTextData.Text);
		var changes = embedder.EmbedRange(changesTextData, x => x.Text).ToList();
		var closestItems = LocalEmbedder.FindClosestWithScore(buildStatus, changes.Select(x => (x.Item, x.Embedding)), 1, 0.2f);
		if (closestItems.Length == 1) {
			var closest = closestItems.First();
			return new BestMatch() {
				ConfidencePercent = Convert.ToInt32(Math.Round(closest.Similarity * 100f)),
				Index = changesTextData.IndexOf(closest.Item)
			};
		}
		return BestMatch.Empty;
	}

	public static BuildInfoTextData ExtractTextData(BuildInfo buildInfo, bool useLog) {
		var problems = string.Join(Environment.NewLine,
			buildInfo.Problems.Select(x => $"{x.ShortSummary} {x.Details}"));
		var testFailures = string.Join(Environment.NewLine,
			buildInfo.FailedTests.Select(t => $"{t.Name}{Environment.NewLine}{t.Details}"));
		var status = $"{buildInfo.StatusText}{Environment.NewLine}{problems}{Environment.NewLine}{testFailures}";
		var changes = buildInfo.Changes
			.GroupBy(x => x.Author).Select(x => {
				var changesText = new StringBuilder();
				foreach (VcsChange change in x) {
					changesText.AppendLine(change.CommitMessage);
					foreach (var modification in change.Modifications) {
						changesText.AppendLine(modification.Path);
					}
				}
				return (x.Key, changesText.ToString());
			});
		if (useLog) {
			status += $"{Environment.NewLine}{buildInfo.Log}";
		}
		return new BuildInfoTextData(status, changes.ToList());
	}

	private static BestMatch FindBestMatchByTfNgrams(TextData source, IList<TextData> itemsToMatch) {
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
		using var transformer = pipeline.Fit(dataView);
		using var predictionEngine = mlContext.Model.CreatePredictionEngine<TextData, TransformedTextData>(transformer);
		TransformedTextData? sourceRes = predictionEngine.Predict(GetSafeText(source));
		var sourceTopic = sourceRes.Features.GetItemWithMaxValue();
		var probabilities = itemsToMatch.Select(item => {
			var prediction = predictionEngine.Predict(GetSafeText(item));
			var sourceTopicWeight = prediction.Features[sourceTopic.Index];
			return sourceTopicWeight;
		}).ToList();
		var bestFit = probabilities.GetItemWithMaxValue();
		var totalProbabilities = probabilities.Sum();
		return totalProbabilities == 0
			? BestMatch.Empty
			: new BestMatch(bestFit.Index, Convert.ToInt32(Math.Round(bestFit.Value * 100.0)));
	}

	private static TextData GetSafeText(TextData source) {
		var maxLength = 112_530;
		var sourceText = source.Text;
		if (sourceText.Length < maxLength) {
			return source;
		}
		var text = TextCompressor.CompressText(sourceText, maxLength);
		return new TextData {
			Text = text
		};
	}

	private class TextData
	{
		public string Text { get; init; } = null!;
	}

	private class TransformedTextData : TextData
	{
		public float[] Features { get; set; } = null!;
	}
}
