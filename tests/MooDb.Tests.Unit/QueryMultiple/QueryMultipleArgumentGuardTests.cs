using Microsoft.Data.SqlClient;

namespace MooDb.Tests.Unit.QueryMultiple;

public sealed class QueryMultipleArgumentGuardTests
{
    [Fact]
    public async Task MooDbQueryMultipleAsync_WhenReadDelegateIsNull_ThrowsArgumentNullException()
    {
        var db = new MooDb("Server=(local);Database=Test;Trusted_Connection=True;TrustServerCertificate=True;");

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            db.QueryMultipleAsync<object>("dbo.usp_Test", null!, cancellationToken: default));
    }

    [Fact]
    public async Task MooSqlQueryMultipleAsync_WhenReadDelegateIsNull_ThrowsArgumentNullException()
    {
        var db = new MooDb("Server=(local);Database=Test;Trusted_Connection=True;TrustServerCertificate=True;");

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            db.Sql.QueryMultipleAsync<object>("SELECT 1;", null!, cancellationToken: default));
    }

    [Fact]
    public async Task SingleAsyncCustomMap_WhenMapIsNull_ThrowsArgumentNullException()
    {
        var db = new MooDb("Server=(local);Database=Test;Trusted_Connection=True;TrustServerCertificate=True;");

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            db.SingleAsync<object>("dbo.usp_Test", (Func<SqlDataReader, object>)null!, cancellationToken: default));
    }

    [Fact]
    public async Task ListAsyncCustomMap_WhenMapIsNull_ThrowsArgumentNullException()
    {
        var db = new MooDb("Server=(local);Database=Test;Trusted_Connection=True;TrustServerCertificate=True;");

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            db.ListAsync<object>("dbo.usp_Test", (Func<SqlDataReader, object>)null!, cancellationToken: default));
    }
}
