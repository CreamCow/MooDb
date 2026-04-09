namespace MooDb.Tests.Unit.Guards;

public sealed class PublicApiArgumentGuardTests
{
    private const string ConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=Fake;Trusted_Connection=True;";

    [Fact]
    public async Task MooDbExecuteAsync_WhenProcedureIsBlank_ThrowsArgumentException()
    {
        // Arrange
        var db = new MooDbContext(ConnectionString);

        // Act
        var action = () => db.ExecuteAsync(" ");

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task MooDbScalarAsync_WhenProcedureIsBlank_ThrowsArgumentException()
    {
        // Arrange
        var db = new MooDbContext(ConnectionString);

        // Act
        var action = () => db.ScalarAsync<int>(" ");

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task MooDbSingleAsync_WhenProcedureIsBlank_ThrowsArgumentException()
    {
        // Arrange
        var db = new MooDbContext(ConnectionString);

        // Act
        var action = () => db.SingleAsync<object>(" ");

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task MooDbListAsync_WhenProcedureIsBlank_ThrowsArgumentException()
    {
        // Arrange
        var db = new MooDbContext(ConnectionString);

        // Act
        var action = () => db.ListAsync<object>(" ");

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task MooDbQueryMultipleAsync_WhenProcedureIsBlank_ThrowsArgumentException()
    {
        // Arrange
        var db = new MooDbContext(ConnectionString);

        // Act
        var action = () => db.QueryMultipleAsync(
            " ",
            read => 0);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task MooSqlExecuteAsync_WhenSqlIsBlank_ThrowsArgumentException()
    {
        // Arrange
        var db = new MooDbContext(ConnectionString);

        // Act
        var action = () => db.Sql.ExecuteAsync(" ");

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task MooSqlScalarAsync_WhenSqlIsBlank_ThrowsArgumentException()
    {
        // Arrange
        var db = new MooDbContext(ConnectionString);

        // Act
        var action = () => db.Sql.ScalarAsync<int>(" ");

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task MooSqlSingleAsync_WhenSqlIsBlank_ThrowsArgumentException()
    {
        // Arrange
        var db = new MooDbContext(ConnectionString);

        // Act
        var action = () => db.Sql.SingleAsync<object>(" ");

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task MooSqlListAsync_WhenSqlIsBlank_ThrowsArgumentException()
    {
        // Arrange
        var db = new MooDbContext(ConnectionString);

        // Act
        var action = () => db.Sql.ListAsync<object>(" ");

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task MooSqlQueryMultipleAsync_WhenSqlIsBlank_ThrowsArgumentException()
    {
        // Arrange
        var db = new MooDbContext(ConnectionString);

        // Act
        var action = () => db.Sql.QueryMultipleAsync(
            " ",
            read => 0);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task MooSqlSingleAsyncCustomMap_WhenMapIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var db = new MooDbContext(ConnectionString);

        // Act
        var action = () => db.Sql.SingleAsync<object>(
            "SELECT 1",
            map: null!);

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(action);
    }

    [Fact]
    public async Task MooSqlListAsyncCustomMap_WhenMapIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var db = new MooDbContext(ConnectionString);

        // Act
        var action = () => db.Sql.ListAsync<object>(
            "SELECT 1",
            map: null!);

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(action);
    }
}
