using Cimon.DB.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Monitor = Cimon.DB.Models.MonitorModel;

namespace Cimon.DB;

public class CimonDbContext : DbContext
{
	private readonly IMediator _mediator;

	public CimonDbContext(IMediator mediator, DbContextOptions<CimonDbContext> options):base(options) {
		_mediator = mediator;
	}
	public DbSet<User> Users { get; set; } = null!;
	public DbSet<Role> Roles { get; set; } = null!;
	public DbSet<Team> Teams { get; set; } = null!;
	public DbSet<BuildConfigModel> BuildConfigurations { get; set; } = null!;
	public DbSet<Monitor> Monitors { get; set; } = null!;
	public DbSet<BuildInMonitor> MonitorBuilds { get; set; } = null!;
	public DbSet<CIConnector> CIConnectors { get; set; } = null!;
	public DbSet<CIConnectorSetting> CIConnectorSettings { get; set; } = null!;

	public DbSet<AppFeatureState> FeatureStates { get; set; } = null!;

	public override async ValueTask<EntityEntry<TEntity>> AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = new CancellationToken()) {
		var entry = await base.AddAsync(entity, cancellationToken);
		await _mediator.Publish(new EntityCreatedNotification<TEntity>(entry), cancellationToken);
		return entry;
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		base.OnModelCreating(modelBuilder);
		modelBuilder.Entity<User>().HasIndex(x => x.Name).IsUnique();
		modelBuilder.Entity<BuildConfigModel>().HasIndex(x => new { x.ConnectorId, x.Id, x.Branch });
		modelBuilder.Entity<Role>().HasMany(x => x.OwnedRoles).WithMany();
		modelBuilder.Entity<Monitor>().Property(x => x.BuildPositions).HasJsonConversion(Database.ProviderName);
		modelBuilder.Entity<BuildConfigModel>().Property(x => x.Props).HasJsonConversion(Database.ProviderName);
		modelBuilder.Entity<BuildConfigModel>().Property(x => x.DemoState).HasJsonConversion(Database.ProviderName);
		modelBuilder.Entity<Team>().HasMany(x => x.ChildTeams).WithMany();
		modelBuilder.Entity<BuildInMonitor>().HasKey(x => new { x.MonitorId, x.BuildConfigId });
		modelBuilder.Entity<BuildConfigModel>().Property(x => x.AllowML).HasDefaultValue(true);
	}
}
