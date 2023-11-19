using Cimon.Contracts.CI;
using Cimon.DB.Models;
using Microsoft.EntityFrameworkCore;
using Monitor = Cimon.DB.Models.MonitorModel;

namespace Cimon.DB;

public class CimonDbContext : DbContext
{
	public CimonDbContext(DbContextOptions<CimonDbContext> options):base(options) {
	}
	public DbSet<User> Users { get; set; } = null!;
	public DbSet<Role> Roles { get; set; } = null!;
	public DbSet<Team> Teams { get; set; } = null!;
	public DbSet<BuildConfig> BuildConfigurations { get; set; } = null!;
	public DbSet<Monitor> Monitors { get; set; } = null!;
	public DbSet<BuildInMonitor> MonitorBuilds { get; set; } = null!;

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		base.OnModelCreating(modelBuilder);
		modelBuilder.Entity<User>().HasIndex(x => x.Name).IsUnique();
		modelBuilder.Entity<BuildConfig>().HasIndex(x => new {x.CISystem, Key = ((BaseBuildConfigInfo)x).Id, x.Branch });
		modelBuilder.Entity<Role>().HasMany(x => x.OwnedRoles).WithMany();
		modelBuilder.Entity<BuildConfig>().Property(x => x.Props).HasJsonConversion();
		modelBuilder.Entity<BuildConfig>().Property(x => x.DemoState).HasJsonConversion();
		modelBuilder.Entity<Team>().HasMany(x => x.ChildTeams).WithMany();
		modelBuilder.Entity<BuildInMonitor>().HasKey(x => new { x.MonitorId, x.BuildConfigId });
	}
}
