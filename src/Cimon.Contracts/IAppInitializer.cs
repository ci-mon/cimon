using System.Reflection;

namespace Cimon.Contracts;

public interface IAppInitializer
{
    Task Init(IServiceProvider serviceProvider);
}

public interface IFeatureAssembly
{
    Assembly Assembly { get; }
}

public class FeatureAssembly<T> : IFeatureAssembly
{
    public Assembly Assembly => typeof(T).Assembly;
}