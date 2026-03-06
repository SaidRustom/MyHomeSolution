using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Infrastructure.Persistence;
using NSubstitute;

namespace MyHomeSolution.Infrastructure.Tests.Testing;

public sealed class InfrastructureTestDbContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public ICurrentUserService CurrentUserService { get; } = Substitute.For<ICurrentUserService>();
    public IDateTimeProvider DateTimeProvider { get; } = Substitute.For<IDateTimeProvider>();

    public InfrastructureTestDbContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        DateTimeProvider.UtcNow.Returns(new DateTimeOffset(2025, 1, 15, 12, 0, 0, TimeSpan.Zero));
        DateTimeProvider.Today.Returns(new DateOnly(2025, 1, 15));
        CurrentUserService.UserId.Returns("test-user-id");

        using var context = new ApplicationDbContext(_options, CurrentUserService, DateTimeProvider);
        context.Database.EnsureCreated();
    }

    public ApplicationDbContext CreateContext(
        ICurrentUserService? currentUserService = null,
        IDateTimeProvider? dateTimeProvider = null)
    {
        return new ApplicationDbContext(
            _options,
            currentUserService ?? CurrentUserService,
            dateTimeProvider ?? DateTimeProvider);
    }

    public void Dispose() => _connection.Dispose();
}
