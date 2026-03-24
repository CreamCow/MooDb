using MooDb.Tests.Integration.Infrastructure.Fixtures;

namespace MooDb.Tests.Integration.Tests.QueryMultiple;

[Collection("MooDb")]
public sealed class SqlQueryMultipleAsyncTests
{
    private readonly MooDbFixture _fixture;

    public SqlQueryMultipleAsyncTests(MooDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task QueryMultipleAsync_WhenUsingSqlSurface_SupportsScalarSingleAndListCombination()
    {
        await _fixture.ResetAsync();

        await _fixture.ExecuteSqlAsync(
            """
            SET IDENTITY_INSERT [dbo].[tbl_User] ON;
            INSERT INTO [dbo].[tbl_User] ([UserId], [Email], [DisplayName], [Age], [IsActive], [CreatedUtc], [UpdatedUtc])
            VALUES (1, N'ada@example.com', N'Ada Lovelace', 36, 1, '2024-01-02T03:04:05', NULL);
            SET IDENTITY_INSERT [dbo].[tbl_User] OFF;

            SET IDENTITY_INSERT [dbo].[tbl_Order] ON;
            INSERT INTO [dbo].[tbl_Order] ([OrderId], [UserId], [OrderNumber], [Total], [CreatedUtc])
            VALUES
                (2, 1, N'ORD-002', 25.50, '2024-01-04T13:14:15'),
                (1, 1, N'ORD-001', 10.25, '2024-01-03T10:11:12');
            SET IDENTITY_INSERT [dbo].[tbl_Order] OFF;
            """);

        var db = _fixture.CreateMooDb();

        var result = await db.Sql.QueryMultipleAsync(
            """
            SELECT COUNT(*) FROM [dbo].[tbl_User] WHERE [UserId] = 1;
            SELECT [UserId], [Email], [DisplayName], [Age], [IsActive], [CreatedUtc], [UpdatedUtc]
            FROM [dbo].[tbl_User]
            WHERE [UserId] = 1;
            SELECT [OrderNumber]
            FROM [dbo].[tbl_Order]
            WHERE [UserId] = 1
            ORDER BY [OrderId];
            """,
            read => new SqlMultiResult
            {
                UserCount = read.Scalar<int>(),
                User = read.Single<SqlUserRow>(),
                OrderNumbers = read.List(static reader => reader.GetString(0))
            });

        Assert.Equal(1, result.UserCount);
        Assert.NotNull(result.User);
        Assert.Equal(1, result.User!.UserId);
        Assert.Equal("Ada Lovelace", result.User.DisplayName);
        Assert.Equal(2, result.OrderNumbers.Count);
        Assert.Equal("ORD-001", result.OrderNumbers[0]);
        Assert.Equal("ORD-002", result.OrderNumbers[1]);
    }

    private sealed class SqlMultiResult
    {
        public int UserCount { get; init; }
        public SqlUserRow? User { get; init; }
        public IReadOnlyList<string> OrderNumbers { get; init; } = [];
    }

    private sealed class SqlUserRow
    {
        public int UserId { get; init; }
        public string Email { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public int? Age { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedUtc { get; init; }
        public DateTime? UpdatedUtc { get; init; }
    }
}
