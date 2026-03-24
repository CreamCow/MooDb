using Microsoft.Data.SqlClient;

namespace MooDb.Tests.Unit.Construction;

public sealed class MooDbConstructionTests
{
    [Fact]
    public void Constructor_WhenConnectionStringIsNull_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new MooDb((string)null!));
    }

    [Fact]
    public void Constructor_WhenConnectionIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new MooDb((SqlConnection)null!));
    }

    [Fact]
    public void Constructor_WhenCreatedFromConnectionString_CreatesSqlSurface()
    {
        var db = new MooDb("Server=(local);Database=Test;Trusted_Connection=True;TrustServerCertificate=True;");

        Assert.NotNull(db.Sql);
    }

    [Fact]
    public void Constructor_WhenCreatedFromConnection_CreatesSqlSurface()
    {
        using var connection = new SqlConnection();
        var db = new MooDb(connection);

        Assert.NotNull(db.Sql);
    }
}
