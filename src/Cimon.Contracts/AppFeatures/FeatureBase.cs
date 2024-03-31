namespace Cimon.Contracts.AppFeatures;

public abstract class FeatureBase
{
    protected FeatureBase() {
        Code = GetType().Name;
    }
    public string Code { get; }
    public virtual bool Enabled => false;
}