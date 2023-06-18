using Cimon.DB.Models;
using Microsoft.EntityFrameworkCore;
using Monitor = Cimon.DB.Models.Monitor;

namespace Cimon.DB;

public class CimonDbContext : DbContext
{
	public CimonDbContext( DbContextOptions<CimonDbContext> options):base(options) {
	}
	public DbSet<User> Users { get; set; } = null!;
	public DbSet<Role> Roles { get; set; } = null!;
	public DbSet<Team> Teams { get; set; } = null!;
	public DbSet<BuildConfig> BuildConfigurations { get; set; } = null!;
	public DbSet<Monitor> Monitors { get; set; } = null!;

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		base.OnModelCreating(modelBuilder);
		modelBuilder.Entity<User>().HasIndex(x => x.Name).IsUnique();
		modelBuilder.Entity<Role>().HasMany(x => x.OwnedRoles).WithMany();
		modelBuilder.Entity<Monitor>().HasMany(x => x.Builds).WithMany().UsingEntity<BuildInMonitor>(
			builder => builder.HasOne(x => x.BuildConfig).WithMany().HasForeignKey(x => x.BuildConfigId),
			builder => builder.HasOne(x => x.Monitor).WithMany().HasForeignKey(x => x.MonitorId));
		modelBuilder.Entity<BuildConfig>().HasMany(x => x.Monitors).WithMany().UsingEntity<BuildInMonitor>(
			builder => builder.HasOne(x => x.Monitor).WithMany().HasForeignKey(x => x.MonitorId),
			builder => builder.HasOne(x => x.BuildConfig).WithMany().HasForeignKey(x => x.BuildConfigId));
		modelBuilder.Entity<BuildConfig>().Property(x => x.Props).HasJsonConversion();
		modelBuilder.Entity<BuildConfig>().Property(x => x.DemoState).HasJsonConversion();
		modelBuilder.Entity<Team>().HasMany(x => x.ChildTeams).WithMany();
	}
}
