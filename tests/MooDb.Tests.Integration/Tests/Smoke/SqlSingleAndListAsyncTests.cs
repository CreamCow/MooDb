using MooDb.Tests.Integration.Infrastructure.Fixtures;
using MooDb.Tests.Integration.Infrastructure.Models;

namespace MooDb.Tests.Integration.Tests.Smoke;

[Collection("MooDb")]
public sealed class SqlSingleAndListAsyncTests
{
    private readonly MooDbFixture _fixture;

    public SqlSingleAndListAsyncTests(MooDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SqlSingleAsync_WhenNoRowsExist_ReturnsNull()
    {
        await _fixture.ResetAsync();

        var db = _fixture.CreateMooDb();

        var user = await db.Sql.SingleAsync<TestUser>(
            """
            SELECT
                [UserId],
                [Email],
                [DisplayName],
                [Age],
                [IsActive],
                [CreatedUtc],
                [UpdatedUtc]
            FROM [dbo].[tbl_User]
            WHERE [UserId] = 999;
            """);

        Assert.Null(user);
    }

    [Fact]
    public async Task SqlSingleAsync_WhenMoreThanOneRowReturned_ThrowsInvalidOperationException()
    {
        await _fixture.ResetAsync();

        await _fixture.ExecuteSqlAsync(
            """
            INSERT INTO [dbo].[tbl_User]
            (
                [Email],
                [DisplayName],
                [Age],
                [IsActive],
                [CreatedUtc],
                [UpdatedUtc]
            )
            VALUES
            (N'ada@example.com', N'Ada Lovelace', 36, 1, '2024-01-02T03:04:05', NULL),
            (N'grace@example.com', N'Grace Hopper', 85, 1, '2024-02-03T04:05:06', NULL);
            """);

        var db = _fixture.CreateMooDb();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            db.Sql.SingleAsync<TestUser>(
                """
                SELECT
                    [UserId],
                    [Email],
                    [DisplayName],
                    [Age],
                    [IsActive],
                    [CreatedUtc],
                    [UpdatedUtc]
                FROM [dbo].[tbl_User]
                ORDER BY [UserId];
                """));
    }

    [Fact]
    public async Task SqlListAsync_WhenNoRowsExist_ReturnsEmptyList()
    {
        await _fixture.ResetAsync();

        var db = _fixture.CreateMooDb();

        var users = await db.Sql.ListAsync<TestUser>(
            """
            SELECT
                [UserId],
                [Email],
                [DisplayName],
                [Age],
                [IsActive],
                [CreatedUtc],
                [UpdatedUtc]
            FROM [dbo].[tbl_User]
            WHERE [UserId] = 999;
            """);

        Assert.NotNull(users);
        Assert.Empty(users);
    }
}
