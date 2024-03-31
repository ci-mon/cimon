using Cimon.Data.Users;
using Microsoft.FeatureManagement.FeatureFilters;

namespace Cimon.Contracts.AppFeatures;

public class TargetingContextAccessor(ICurrentUserAccessor currentUserAccessor) : ITargetingContextAccessor
{
    public async ValueTask<TargetingContext> GetContextAsync() {
        var user = await currentUserAccessor.Current;
        var targetingContext = new TargetingContext {
            UserId = user.Id.ToString(),
            Groups = user.Teams
        };
        return targetingContext;
    }
}