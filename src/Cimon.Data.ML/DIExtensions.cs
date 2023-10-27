namespace Cimon.Data.ML;

using Microsoft.Extensions.DependencyInjection;

public static class DIExtensions
{
	public static IServiceCollection AddCimonML(this IServiceCollection serviceCollection) {
		return serviceCollection.AddSingleton<IBuildFailurePredictor, BuildFailurePredictor>();
	}
}
