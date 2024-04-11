using Cimon.Contracts.AppFeatures;

namespace Cimon.Data.ML;

public static class MlFeatures
{
    public class UseSmartComponentsToFindFailureSuspect : FeatureBase
    {
        public override bool Enabled => false;
    }
}