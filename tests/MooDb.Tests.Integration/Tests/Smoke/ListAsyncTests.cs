using Microsoft.Data.SqlClient;
using MooDb.Tests.Integration.Infrastructure.Fixtures;
using MooDb.Tests.Integration.Infrastructure.Models;

namespace MooDb.Tests.Integration.Tests.Smoke;

[Collection("MooDb")]
public sealed class ListAsyncTests
{
    private readonly MooDbFixture _fixture;

    public ListAsyncTests(MooDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ListAsync_WhenNoRowsExist_ReturnsEmptyList()
    {
        // Arrange
        await _fixture.ResetAsync();

        var db = _fixture.CreateMooDb();

        // Act
        var users = await db.ListAsync<TestUser>("dbo.usp_User_List");

        // Assert
        Assert.NotNull(users);
        Assert.Empty(users);
    }

    [Fact]
    public async Task ListAsync_WhenRowsExist_ReturnsUsersInUserIdOrder()
    {
        // Arrange
        await _fixture.ResetAsync();

        var firstCreatedUtc = new DateTime(2024, 01, 02, 03, 04, 05);
        var secondCreatedUtc = new DateTime(2024, 02, 03, 04, 05, 06);

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
                @UserId1,
                @Email1,
                @DisplayName1,
                @Age1,
                @IsActive1,
                @CreatedUtc1,
                @UpdatedUtc1
            ),
            (
                @UserId2,
                @Email2,
                @DisplayName2,
                @Age2,
                @IsActive2,
                @CreatedUtc2,
                @UpdatedUtc2
            );

            SET IDENTITY_INSERT [dbo].[tbl_User] OFF;
            """,
            new[]
            {
                new SqlParameter("@UserId1", 2),
                new SqlParameter("@Email1", "grace.hopper@example.com"),
                new SqlParameter("@DisplayName1", "Grace Hopper"),
                new SqlParameter("@Age1", 85),
                new SqlParameter("@IsActive1", true),
                new SqlParameter("@CreatedUtc1", secondCreatedUtc),
                new SqlParameter("@UpdatedUtc1", DBNull.Value),

                new SqlParameter("@UserId2", 1),
                new SqlParameter("@Email2", "ada.lovelace@example.com"),
                new SqlParameter("@DisplayName2", "Ada Lovelace"),
                new SqlParameter("@Age2", 36),
                new SqlParameter("@IsActive2", true),
                new SqlParameter("@CreatedUtc2", firstCreatedUtc),
                new SqlParameter("@UpdatedUtc2", DBNull.Value)
            });

        var db = _fixture.CreateMooDb();

        // Act
        var users = await db.ListAsync<TestUser>("dbo.usp_User_List");

        // Assert
        Assert.NotNull(users);
        Assert.Equal(2, users.Count);

        Assert.Equal(1, users[0].UserId);
        Assert.Equal("ada.lovelace@example.com", users[0].Email);
        Assert.Equal("Ada Lovelace", users[0].DisplayName);
        Assert.Equal(36, users[0].Age);
        Assert.True(users[0].IsActive);
        Assert.Equal(firstCreatedUtc, users[0].CreatedUtc);
        Assert.Null(users[0].UpdatedUtc);

        Assert.Equal(2, users[1].UserId);
        Assert.Equal("grace.hopper@example.com", users[1].Email);
        Assert.Equal("Grace Hopper", users[1].DisplayName);
        Assert.Equal(85, users[1].Age);
        Assert.True(users[1].IsActive);
        Assert.Equal(secondCreatedUtc, users[1].CreatedUtc);
        Assert.Null(users[1].UpdatedUtc);
    }
}
