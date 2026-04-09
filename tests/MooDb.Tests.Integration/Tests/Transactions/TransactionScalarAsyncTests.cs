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
    public async Task ScalarAsync_WhenSqlNullReturnedInsideTransactionAndTypeIsNonNullable_ReturnsDefaultValue()
    {
        // Arrange
        await _fixture.ResetAsync();

        var db = _fixture.CreateMooDbContext();
        await using var transaction = await db.BeginTransactionAsync();

        // Act
        var value = await transaction.Sql.ScalarAsync<int>("SELECT CAST(NULL AS int);");

        // Assert
        Assert.Equal(0, value);
    }

    [Fact]
    public async Task ScalarAsync_WhenSqlNullReturnedInsideTransactionAndTypeIsNullable_ReturnsNull()
    {
        // Arrange
        await _fixture.ResetAsync();

        var db = _fixture.CreateMooDbContext();
        await using var transaction = await db.BeginTransactionAsync();

        // Act
        var value = await transaction.Sql.ScalarAsync<int?>("SELECT CAST(NULL AS int);");

        // Assert
        Assert.Null(value);
    }
}
