using MooDb.Tests.Integration.Infrastructure.Fixtures;
using MooDb.Tests.Integration.Infrastructure.Models;

namespace MooDb.Tests.Integration.Tests.Transactions;

[Collection("MooDb")]
public sealed class TransactionQueryMultipleAsyncTests
{
    private readonly MooDbFixture _fixture;

    public TransactionQueryMultipleAsyncTests(MooDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task QueryMultipleAsync_WhenExecutedInsideTransaction_ReturnsExpectedShape()
    {
        // Arrange
        await _fixture.ResetAsync();

        await _fixture.ExecuteSqlAsync(
            """
            SET IDENTITY_INSERT [dbo].[tbl_User] ON;
            INSERT INTO [dbo].[tbl_User] ([UserId], [Email], [DisplayName], [Age], [IsActive], [CreatedUtc], [UpdatedUtc])
            VALUES (1, N'ada@example.com', N'Ada', 36, 1, '2024-01-02T03:04:05', NULL);
            SET IDENTITY_INSERT [dbo].[tbl_User] OFF;

            SET IDENTITY_INSERT [dbo].[tbl_Order] ON;
            INSERT INTO [dbo].[tbl_Order] ([OrderId], [UserId], [OrderNumber], [Total], [CreatedUtc])
            VALUES (1, 1, N'ORD-001', 12.34, '2024-01-03T04:05:06');
            SET IDENTITY_INSERT [dbo].[tbl_Order] OFF;
            """);

        var db = _fixture.CreateMooDb();
        await using var transaction = await db.BeginTransactionAsync();
        var parameters = new MooParams().AddInt("@UserId", 1);

        // Act
        var result = await transaction.QueryMultipleAsync(
            "Tests.usp_QueryMultiple_UserAndOrders",
            read => new TransactionUserAndOrdersResult
            {
                User = read.Single<TestUser>(),
                Orders = read.List<TestOrder>()
            },
            parameters);

        // Assert
        Assert.NotNull(result.User);
        Assert.Equal(1, result.User!.UserId);
        Assert.Single(result.Orders);
        Assert.Equal("ORD-001", result.Orders[0].OrderNumber);
    }

    private sealed class TransactionUserAndOrdersResult
    {
        public TestUser? User { get; init; }
        public IReadOnlyList<TestOrder> Orders { get; init; } = [];
    }
}
