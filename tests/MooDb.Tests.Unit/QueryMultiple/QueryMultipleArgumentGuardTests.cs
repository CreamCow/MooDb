using Microsoft.Data.SqlClient;

namespace MooDb.Tests.Unit.QueryMultiple;

public sealed class QueryMultipleArgumentGuardTests
{
    [Fact]
    public async Task MooDbQueryMultipleAsync_WhenReadDelegateIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var db = new MooDbContext("Server=(local);Database=Test;Trusted_Connection=True;TrustServerCertificate=True;");

        // Act
        var action = () => db.QueryMultipleAsync<object>("dbo.usp_Test", null!, cancellationToken: default);

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(action);
    }

    [Fact]
    public async Task MooSqlQueryMultipleAsync_WhenReadDelegateIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var db = new MooDbContext("Server=(local);Database=Test;Trusted_Connection=True;TrustServerCertificate=True;");

        // Act
        var action = () => db.Sql.QueryMultipleAsync<object>("SELECT 1;", null!, cancellationToken: default);

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(action);
    }

    [Fact]
    public async Task SingleAsyncCustomMap_WhenMapIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var db = new MooDbContext("Server=(local);Database=Test;Trusted_Connection=True;TrustServerCertificate=True;");

        // Act
        var action = () => db.SingleAsync<object>("dbo.usp_Test", (Func<SqlDataReader, object>)null!, cancellationToken: default);

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(action);
    }

    [Fact]
    public async Task ListAsyncCustomMap_WhenMapIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var db = new MooDbContext("Server=(local);Database=Test;Trusted_Connection=True;TrustServerCertificate=True;");

        // Act
        var action = () => db.ListAsync<object>("dbo.usp_Test", (Func<SqlDataReader, object>)null!, cancellationToken: default);

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(action);
    }
}
