using Microsoft.Data.SqlClient;
using MooDb.Tests.Integration.Infrastructure.Fixtures;
using MooDb.Tests.Integration.Infrastructure.Models;

namespace MooDb.Tests.Integration.Tests.Smoke;

[Collection("MooDb")]
public sealed class QueryMultipleAsyncTests
{
    private readonly MooDbFixture _fixture;

    public QueryMultipleAsyncTests(MooDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task QueryMultipleAsync_WhenUserAndOrdersExist_ReturnsExpectedResultSets()
    {
        // Arrange
        await _fixture.ResetAsync();

        const int userId = 1;
        var userCreatedUtc = new DateTime(2024, 01, 02, 03, 04, 05);
        var firstOrderCreatedUtc = new DateTime(2024, 01, 03, 10, 11, 12);
        var secondOrderCreatedUtc = new DateTime(2024, 01, 04, 13, 14, 15);

        await _fixture.ExecuteSqlAsync(
            """
            SET IDENTITY_INSERT [dbo].[tbl_User] ON;

            INSERT INTO [dbo].[tbl_User]
            (
                [UserId],
                [Email],
                [DisplayName],
                [Age],
                [IsActive],
                [CreatedUtc],
                [UpdatedUtc]
            )
            VALUES
            (
                @UserId,
                @Email,
                @DisplayName,
                @Age,
                @IsActive,
                @UserCreatedUtc,
                @UpdatedUtc
            );

            SET IDENTITY_INSERT [dbo].[tbl_User] OFF;

            SET IDENTITY_INSERT [dbo].[tbl_Order] ON;

            INSERT INTO [dbo].[tbl_Order]
            (
                [OrderId],
                [UserId],
                [OrderNumber],
                [Total],
                [CreatedUtc]
            )
            VALUES
            (
                @OrderId1,
                @UserId,
                @OrderNumber1,
                @Total1,
                @OrderCreatedUtc1
            ),
            (
                @OrderId2,
                @UserId,
                @OrderNumber2,
                @Total2,
                @OrderCreatedUtc2
            );

            SET IDENTITY_INSERT [dbo].[tbl_Order] OFF;
            """,
            [
                new SqlParameter("@UserId", userId),
                new SqlParameter("@Email", "ada.lovelace@example.com"),
                new SqlParameter("@DisplayName", "Ada Lovelace"),
                new SqlParameter("@Age", 36),
                new SqlParameter("@IsActive", true),
                new SqlParameter("@UserCreatedUtc", userCreatedUtc),
                new SqlParameter("@UpdatedUtc", DBNull.Value),

                new SqlParameter("@OrderId1", 2),
                new SqlParameter("@OrderNumber1", "ORD-002"),
                new SqlParameter("@Total1", 25.50m),
                new SqlParameter("@OrderCreatedUtc1", secondOrderCreatedUtc),

                new SqlParameter("@OrderId2", 1),
                new SqlParameter("@OrderNumber2", "ORD-001"),
                new SqlParameter("@Total2", 10.25m),
                new SqlParameter("@OrderCreatedUtc2", firstOrderCreatedUtc)
            ]);

        var db = _fixture.CreateMooDbContext();
        var parameters = new MooParams().AddInt("@UserId", userId);

        // Act
        var result = await db.QueryMultipleAsync(
            "Tests.usp_QueryMultiple_UserAndOrders",
            read => new UserAndOrdersResult
            {
                User = read.Single<TestUser>(),
                Orders = read.List<TestOrder>()
            },
            parameters);

        // Assert
        Assert.NotNull(result.User);
        Assert.Equal(userId, result.User!.UserId);
        Assert.Equal("ada.lovelace@example.com", result.User.Email);
        Assert.Equal("Ada Lovelace", result.User.DisplayName);
        Assert.Equal(36, result.User.Age);
        Assert.True(result.User.IsActive);
        Assert.Equal(userCreatedUtc, result.User.CreatedUtc);
        Assert.Null(result.User.UpdatedUtc);

        Assert.NotNull(result.Orders);
        Assert.Equal(2, result.Orders.Count);

        Assert.Equal(1, result.Orders[0].OrderId);
        Assert.Equal(userId, result.Orders[0].UserId);
        Assert.Equal("ORD-001", result.Orders[0].OrderNumber);
        Assert.Equal(10.25m, result.Orders[0].Total);
        Assert.Equal(firstOrderCreatedUtc, result.Orders[0].CreatedUtc);

        Assert.Equal(2, result.Orders[1].OrderId);
        Assert.Equal(userId, result.Orders[1].UserId);
        Assert.Equal("ORD-002", result.Orders[1].OrderNumber);
        Assert.Equal(25.50m, result.Orders[1].Total);
        Assert.Equal(secondOrderCreatedUtc, result.Orders[1].CreatedUtc);
    }

    [Fact]
    public async Task QueryMultipleAsync_WhenReadPastAvailableResultSets_ThrowsInvalidOperationException()
    {
        // Arrange
        await _fixture.ResetAsync();

        const int userId = 1;

        await _fixture.ExecuteSqlAsync(
            """
            SET IDENTITY_INSERT [dbo].[tbl_User] ON;

            INSERT INTO [dbo].[tbl_User]
            (
                [UserId],
                [Email],
                [DisplayName],
                [Age],
                [IsActive],
                [CreatedUtc],
                [UpdatedUtc]
            )
            VALUES
            (
                @UserId,
                @Email,
                @DisplayName,
                @Age,
                @IsActive,
                @CreatedUtc,
                @UpdatedUtc
            );

            SET IDENTITY_INSERT [dbo].[tbl_User] OFF;
            """,
            [
                new SqlParameter("@UserId", userId),
                new SqlParameter("@Email", "ada.lovelace@example.com"),
                new SqlParameter("@DisplayName", "Ada Lovelace"),
                new SqlParameter("@Age", 36),
                new SqlParameter("@IsActive", true),
                new SqlParameter("@CreatedUtc", new DateTime(2024, 01, 02, 03, 04, 05)),
                new SqlParameter("@UpdatedUtc", DBNull.Value)
            ]);

        var db = _fixture.CreateMooDbContext();
        var parameters = new MooParams().AddInt("@UserId", userId);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await db.QueryMultipleAsync(
                "Tests.usp_QueryMultiple_UserAndOrders",
                read =>
                {
                    _ = read.Single<TestUser>();
                    _ = read.List<TestOrder>();
                    _ = read.Scalar<int>();

                    return 0;
                },
                parameters));

        // Assert
        Assert.Equal("No more result sets are available.", ex.Message);
    }

    private sealed class UserAndOrdersResult
    {
        public TestUser? User { get; init; }
        public IReadOnlyList<TestOrder> Orders { get; init; } = [];
    }
}
