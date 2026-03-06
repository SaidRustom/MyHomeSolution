using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MyHomeSolution.Application.Tests.Testing;

public sealed class TestDbContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TestDbContext> _options;

    public TestDbContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new TestDbContext(_options);
        context.Database.EnsureCreated();
    }

    public TestDbContext CreateContext() => new(_options);

    public void Dispose() => _connection.Dispose();
}
