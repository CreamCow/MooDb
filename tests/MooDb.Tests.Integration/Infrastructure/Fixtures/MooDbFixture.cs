using Microsoft.Data.SqlClient;

namespace MooDb.Tests.Integration.Infrastructure.Fixtures;

public sealed class MooDbFixture : IAsyncLifetime
{
    public string ConnectionString { get; private set; } = string.Empty;

    public Task InitializeAsync()
    {
        throw new NotImplementedException();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    public MooDb CreateMooDb()
    {
        throw new NotImplementedException();
    }

    public MooDb CreateStrictMooDb()
    {
        throw new NotImplementedException();
    }

    public Task<SqlConnection> CreateOpenConnectionAsync()
    {
        throw new NotImplementedException();
    }

    public Task ResetAsync()
    {
        throw new NotImplementedException();
    }

    public Task<int> ExecuteSqlAsync(string sql)
    {
        throw new NotImplementedException();
    }

    public Task<int> ExecuteSqlAsync(string sql, IEnumerable<SqlParameter> parameters)
    {
        throw new NotImplementedException();
    }

    public Task<T?> ScalarSqlAsync<T>(string sql)
    {
        throw new NotImplementedException();
    }

    public Task<T?> ScalarSqlAsync<T>(string sql, IEnumerable<SqlParameter> parameters)
    {
        throw new NotImplementedException();
    }
}