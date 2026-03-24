using MooDb.Tests.Integration.Infrastructure.Fixtures;

namespace MooDb.Tests.Integration.Tests.Transactions;

[Collection("MooDb")]
public sealed class TransactionScalarAsyncTests
{
    private readonly MooDbFixture _fixture;

    public TransactionScalarAsyncTests(MooDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ScalarOrDefaultAsync_WhenSqlNullReturnedInsideTransaction_ReturnsDefault()
    {
        await _fixture.ResetAsync();

        var db = _fixture.CreateMooDb();
        await using var transaction = await db.BeginTransactionAsync();

        var value = await transaction.Sql.ScalarOrDefaultAsync<int>("SELECT CAST(NULL AS int);");

        Assert.Equal(0, value);
    }
}
