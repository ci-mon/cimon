using Cimon.Contracts.AppFeatures;

namespace Cimon.Data.ML;

public class MlFeatures
{
    public class UseSmartComponents : FeatureBase
    {
        public override bool Enabled => false;
    }
}