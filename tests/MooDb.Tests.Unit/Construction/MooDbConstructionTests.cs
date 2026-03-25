using Microsoft.Data.SqlClient;

namespace MooDb.Tests.Unit.Construction;

public sealed class MooDbConstructionTests
{
    [Fact]
    public void Constructor_WhenConnectionStringIsNull_ThrowsArgumentException()
    {
        // Arrange

        // Act
        var action = () => new MooDb((string)null!);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Constructor_WhenConnectionIsNull_ThrowsArgumentNullException()
    {
        // Arrange

        // Act
        var action = () => new MooDb((SqlConnection)null!);

        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Constructor_WhenCreatedFromConnectionString_CreatesSqlSurface()
    {
        // Arrange

        // Act
        var db = new MooDb("Server=(local);Database=Test;Trusted_Connection=True;TrustServerCertificate=True;");

        // Assert
        Assert.NotNull(db.Sql);
        Assert.NotNull(db.Bulk);
    }

    [Fact]
    public void Constructor_WhenCreatedFromConnection_CreatesSqlSurface()
    {
        // Arrange
        using var connection = new SqlConnection();

        // Act
        var db = new MooDb(connection);

        // Assert
        Assert.NotNull(db.Sql);
        Assert.NotNull(db.Bulk);
    }
}