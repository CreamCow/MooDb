using MooDb.Tests.Integration.Infrastructure.Fixtures;

namespace MooDb.Tests.Integration.Tests.Transactions;

[Collection("MooDb")]
public sealed class TransactionExecuteAsyncTests
{
    private readonly MooDbFixture _fixture;

    public TransactionExecuteAsyncTests(MooDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ExecuteAsync_WhenCommitted_PersistsChanges()
    {
        // Arrange
        await _fixture.ResetAsync();

        var db = _fixture.CreateMooDb();
        await using var transaction = await db.BeginTransactionAsync();

        // Act
        var affectedRows = await transaction.ExecuteAsync(
            "dbo.usp_User_Insert",
            new MooParams()
                .AddNVarChar("@Email", "commit@example.com", 320)
                .AddNVarChar("@DisplayName", "Committed User", 200)
                .AddInt("@Age", 40)
                .AddBit("@IsActive", true)
                .AddDateTime2("@CreatedUtc", new DateTime(2024, 01, 02, 03, 04, 05), 7)
                .AddDateTime2("@UpdatedUtc", null, 7));

        await transaction.CommitAsync();

        // Assert
        var userCount = await _fixture.ScalarSqlAsync<int>("SELECT COUNT(*) FROM [dbo].[tbl_User];");
        var displayName = await _fixture.ScalarSqlAsync<string>("SELECT TOP (1) [DisplayName] FROM [dbo].[tbl_User];");

        Assert.Equal(1, affectedRows);
        Assert.Equal(1, userCount);
        Assert.Equal("Committed User", displayName);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUsedAfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        await _fixture.ResetAsync();

        var db = _fixture.CreateMooDb();
        var transaction = await db.BeginTransactionAsync();
        await transaction.DisposeAsync();

        // Act
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            transaction.ExecuteAsync("dbo.usp_User_Count"));
    }


    [Fact]
    public async Task ExecuteAsync_WhenUsedAfterCommit_ThrowsInvalidOperationException()
    {
        // Arrange
        await _fixture.ResetAsync();

        var db = _fixture.CreateMooDb();
        await using var transaction = await db.BeginTransactionAsync();
        await transaction.CommitAsync();

        // Act
        var action = () => transaction.ExecuteAsync("dbo.usp_User_Count");

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(action);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDisposedWithoutCommit_RollsBackChanges()
    {
        // Arrange
        await _fixture.ResetAsync();

        var db = _fixture.CreateMooDb();

        // Act
        await using (var transaction = await db.BeginTransactionAsync())
        {
            var affectedRows = await transaction.ExecuteAsync(
                "dbo.usp_User_Insert",
                new MooParams()
                    .AddNVarChar("@Email", "rollback@example.com", 320)
                    .AddNVarChar("@DisplayName", "Rolled Back User", 200)
                    .AddInt("@Age", 41)
                    .AddBit("@IsActive", true)
                    .AddDateTime2("@CreatedUtc", new DateTime(2024, 01, 02, 03, 04, 05), 7)
                    .AddDateTime2("@UpdatedUtc", null, 7));

            Assert.Equal(1, affectedRows);
        }

        // Assert
        var userCount = await _fixture.ScalarSqlAsync<int>("SELECT COUNT(*) FROM [dbo].[tbl_User];");

        Assert.Equal(0, userCount);
    }
}
