using Microsoft.FeatureManagement;

namespace Cimon.Contracts.AppFeatures;

public class FeatureInstanceCache<TFeature> where TFeature : FeatureBase, new()
{
    static FeatureInstanceCache() {
        Instance = new TFeature();
    }
    public static FeatureBase Instance { get; }
}

public static class Utils
{
 
    public static async Task<bool> IsEnabled<TFeature>(this IFeatureManager manager) where TFeature: FeatureBase, new() {
        return await manager.IsEnabledAsync(FeatureInstanceCache<TFeature>.Instance.Code);
    }
}