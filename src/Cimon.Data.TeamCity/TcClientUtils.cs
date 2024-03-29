﻿using TeamCityAPI.Models.Common;
using TeamCityAPI.Queries.Interfaces;

namespace Cimon.Data.TeamCity;

public static class TcClientUtils
{
	public static async IAsyncEnumerable<TEntity> GetAsyncEnumerable<TPage, TEntity>(
			this ITcPagedQuery<TPage>query, int pageSize = 100) where TPage : Page<TEntity> where TEntity : TcModel {
		Page<TEntity> page = await query.GetAsync(pageSize);
		while (true) {
			foreach (var buildType in page.Value) {
				yield return buildType;
			}
			if (page.NextHref is null) {
				yield break;
			}
			page = await page.GetNextAsync();
		}
	}
}
