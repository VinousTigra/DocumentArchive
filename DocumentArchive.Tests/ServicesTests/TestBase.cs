using DocumentArchive.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DocumentArchive.Tests.ServicesTests;

public abstract class TestBase : IDisposable
{
    private readonly SqliteConnection _connection;
    protected readonly AppDbContext Context;

    protected TestBase()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        Context = new AppDbContext(options);
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Close();
        _connection.Dispose();
    }
}