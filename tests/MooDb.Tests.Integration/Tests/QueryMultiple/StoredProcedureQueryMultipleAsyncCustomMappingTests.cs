using Microsoft.Data.SqlClient;
using MooDb.Tests.Integration.Infrastructure.Fixtures;

namespace MooDb.Tests.Integration.Tests.QueryMultiple;

[Collection("MooDb")]
public sealed class StoredProcedureQueryMultipleAsyncCustomMappingTests
{
    private readonly MooDbFixture _fixture;

    public StoredProcedureQueryMultipleAsyncCustomMappingTests(MooDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task QueryMultipleAsync_WhenCustomRowMappersAreUsed_ReturnsExpectedProjection()
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

        var result = await db.QueryMultipleAsync(
            "Tests.usp_QueryMultiple_UserAndOrders",
            read => new CustomMappedUserAndOrdersResult
            {
                User = read.Single(static reader => new UserHeader(
                    reader.GetInt32(reader.GetOrdinal("UserId")),
                    reader.GetString(reader.GetOrdinal("DisplayName")))),
                Orders = read.List(static reader => new OrderLine(
                    reader.GetInt32(reader.GetOrdinal("OrderId")),
                    reader.GetString(reader.GetOrdinal("OrderNumber")),
                    reader.GetDecimal(reader.GetOrdinal("Total"))))
            },
            new[]
            {
                new SqlParameter("@UserId", 1)
            });

        Assert.NotNull(result.User);
        Assert.Equal(1, result.User!.UserId);
        Assert.Equal("Ada Lovelace", result.User.DisplayName);
        Assert.Equal(2, result.Orders.Count);
        Assert.Equal(1, result.Orders[0].OrderId);
        Assert.Equal("ORD-001", result.Orders[0].OrderNumber);
        Assert.Equal(10.25m, result.Orders[0].Total);
        Assert.Equal(2, result.Orders[1].OrderId);
    }

    private sealed class CustomMappedUserAndOrdersResult
    {
        public UserHeader? User { get; init; }
        public IReadOnlyList<OrderLine> Orders { get; init; } = [];
    }

    private sealed record UserHeader(int UserId, string DisplayName);
    private sealed record OrderLine(int OrderId, string OrderNumber, decimal Total);
}
