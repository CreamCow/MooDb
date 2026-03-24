using MooDb.Tests.Unit.Guards;

namespace MooDb.Tests.Unit.Guards;

public sealed class PublicApiArgumentGuardTests
{
    private const string ConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=Fake;Trusted_Connection=True;";

    [Fact]
    public async Task MooDbExecuteAsync_WhenProcedureIsBlank_ThrowsArgumentException()
    {
        var db = new MooDb(ConnectionString);

        await Assert.ThrowsAsync<ArgumentException>(() => db.ExecuteAsync(" "));
    }

    [Fact]
    public async Task MooDbScalarAsync_WhenProcedureIsBlank_ThrowsArgumentException()
    {
        var db = new MooDb(ConnectionString);

        await Assert.ThrowsAsync<ArgumentException>(() => db.ScalarAsync<int>(" "));
    }

    [Fact]
    public async Task MooDbSingleAsync_WhenProcedureIsBlank_ThrowsArgumentException()
    {
        var db = new MooDb(ConnectionString);

        await Assert.ThrowsAsync<ArgumentException>(() => db.SingleAsync<object>(" "));
    }

    [Fact]
    public async Task MooDbListAsync_WhenProcedureIsBlank_ThrowsArgumentException()
    {
        var db = new MooDb(ConnectionString);

        await Assert.ThrowsAsync<ArgumentException>(() => db.ListAsync<object>(" "));
    }

    [Fact]
    public async Task MooDbQueryMultipleAsync_WhenProcedureIsBlank_ThrowsArgumentException()
    {
        var db = new MooDb(ConnectionString);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            db.QueryMultipleAsync(
                " ",
                read => 0));
    }

    [Fact]
    public async Task MooSqlExecuteAsync_WhenSqlIsBlank_ThrowsArgumentException()
    {
        var db = new MooDb(ConnectionString);

        await Assert.ThrowsAsync<ArgumentException>(() => db.Sql.ExecuteAsync(" "));
    }

    [Fact]
    public async Task MooSqlScalarAsync_WhenSqlIsBlank_ThrowsArgumentException()
    {
        var db = new MooDb(ConnectionString);

        await Assert.ThrowsAsync<ArgumentException>(() => db.Sql.ScalarAsync<int>(" "));
    }

    [Fact]
    public async Task MooSqlSingleAsync_WhenSqlIsBlank_ThrowsArgumentException()
    {
        var db = new MooDb(ConnectionString);

        await Assert.ThrowsAsync<ArgumentException>(() => db.Sql.SingleAsync<object>(" "));
    }

    [Fact]
    public async Task MooSqlListAsync_WhenSqlIsBlank_ThrowsArgumentException()
    {
        var db = new MooDb(ConnectionString);

        await Assert.ThrowsAsync<ArgumentException>(() => db.Sql.ListAsync<object>(" "));
    }

    [Fact]
    public async Task MooSqlQueryMultipleAsync_WhenSqlIsBlank_ThrowsArgumentException()
    {
        var db = new MooDb(ConnectionString);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            db.Sql.QueryMultipleAsync(
                " ",
                read => 0));
    }

    [Fact]
    public async Task MooSqlSingleAsyncCustomMap_WhenMapIsNull_ThrowsArgumentNullException()
    {
        var db = new MooDb(ConnectionString);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            db.Sql.SingleAsync<object>(
                "SELECT 1",
                map: null!));
    }

    [Fact]
    public async Task MooSqlListAsyncCustomMap_WhenMapIsNull_ThrowsArgumentNullException()
    {
        var db = new MooDb(ConnectionString);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            db.Sql.ListAsync<object>(
                "SELECT 1",
                map: null!));
    }
}