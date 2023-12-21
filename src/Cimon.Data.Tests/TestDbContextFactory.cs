using Cimon.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Cimon.Data.Tests;

public class TestDbContextFactory : IDbContextFactory<CimonDbContext>
{
	private readonly DbContextOptions<CimonDbContext> _options;
	private CimonDbContext? _context;
	public static TestDbContextFactory New => new();
	public TestDbContextFactory(string databaseName = "InMemoryTest")
	{
		_options = new DbContextOptionsBuilder<CimonDbContext>()
			.UseInMemoryDatabase(databaseName)
			.Options;
	}

	public CimonDbContext Context => _context ??= new CimonDbContext(Substitute.For<IMediator>(), _options);

	public CimonDbContext CreateDbContext() => Context;
}
