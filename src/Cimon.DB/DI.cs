﻿using Cimon.DB;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class CimonDbExtensions
{
	public static IServiceCollection AddCimonDb(this IServiceCollection services, IConfiguration configuration,
		bool isDevelopment, IHealthChecksBuilder health) {
		Enum.TryParse(configuration.GetSection("DbProvider").Value, out DbType dbType);
		var dbTypeName = dbType.ToString();
		var connectionString = configuration.GetConnectionString(dbTypeName);
		if (dbType == DbType.SqlServer) {
			health.AddSqlServer(connectionString!);
		} else {
			health.AddSqlite(connectionString!);
		}
		return services
			.AddDbContextFactory<CimonDbContext>(options => { 
				var migrationAssembly = $"Cimon.DB.Migrations.{dbTypeName}";
				if (dbType == DbType.SqlServer) {
					options.UseSqlServer(connectionString, x => x.MigrationsAssembly(migrationAssembly)
						.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
				} else {
					options.UseSqlite(connectionString, x => x.MigrationsAssembly(migrationAssembly)
						.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
				}
				options.EnableSensitiveDataLogging().EnableDetailedErrors();
			})
			.AddScoped<DbInitializer>()
			.Configure<DbSeedOptions>(options => options.UseTestData = isDevelopment);
	}
}
