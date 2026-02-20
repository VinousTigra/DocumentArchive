using DocumentArchive.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DocumentArchive.Tests.ServicesTests;

public abstract class TestBase : IDisposable
{
    protected readonly AppDbContext Context;
    private readonly SqliteConnection _connection;

    protected TestBase()
    {
        // Создаём in-memory SQLite с уникальным именем для каждого теста
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open(); // Важно: держать открытым, чтобы БД существовала

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new AppDbContext(options);
        Context.Database.EnsureCreated();
        SeedData();
    }

    protected virtual void SeedData() { }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Close();
        _connection.Dispose();
    }
}