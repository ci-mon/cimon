using Microsoft.EntityFrameworkCore;

namespace Cimon.DB;

public class CimonDbContext : DbContext
{
	public CimonDbContext( DbContextOptions<CimonDbContext> options):base(options) {
	}
	public DbSet<User> Users { get; set; } = null!;
	public DbSet<Role> Roles { get; set; } = null!;
	public DbSet<Team> Teams { get; set; } = null!;

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		base.OnModelCreating(modelBuilder);
		modelBuilder.Entity<User>().HasIndex(x => x.Name).IsUnique();
		modelBuilder.Entity<Role>().HasMany(x => x.OwnedRoles).WithMany();
	}
}
