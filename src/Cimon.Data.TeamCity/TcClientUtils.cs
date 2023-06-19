using TeamCityAPI.Models.Common;
using TeamCityAPI.Queries.Interfaces;

namespace Cimon.Data.TeamCity;

public static class TcClientUtils
{
	public static async IAsyncEnumerable<TEntity> GetAsyncEnumerable<TPage, TEntity>(
			this ITcPagedIncludableQuery<TPage, ICollection<TEntity>>query) 
			where TEntity : TcModel where TPage : Page<TEntity> {
		var page = await query.GetAsync();
		while (true) {
			foreach (var buildType in page.Value) {
				yield return buildType;
			}
			if (page.NextHref is null) {
				yield break;
			}
			page = (TPage)await page.GetNextAsync();
		}
	}
}