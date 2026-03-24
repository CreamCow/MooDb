using Microsoft.Data.SqlClient;
using MooDb.Tests.Integration.Infrastructure.Fixtures;
using MooDb.Tests.Integration.Infrastructure.Models;

namespace MooDb.Tests.Integration.Tests.Smoke;

[Collection("MooDb")]
public sealed class SingleAsyncTests
{
    private readonly MooDbFixture _fixture;

    public SingleAsyncTests(MooDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SingleAsync_WhenRowExists_ReturnsMappedUser()
    {
        // Arrange
        await _fixture.ResetAsync();

        const int userId = 1;
        var createdUtc = new DateTime(2024, 01, 02, 03, 04, 05);

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
            new SqlParameter("@CreatedUtc", createdUtc),
            new SqlParameter("@UpdatedUtc", DBNull.Value)
            ]);

        var db = _fixture.CreateMooDb();

        var parameters = new MooParams()
            .AddInt("@UserId", userId);

        // Act
        var user = await db.SingleAsync<TestUser>(
            "dbo.usp_User_GetById",
            parameters);

        // Assert
        Assert.NotNull(user);
        Assert.Equal(userId, user.UserId);
        Assert.Equal("ada.lovelace@example.com", user.Email);
        Assert.Equal("Ada Lovelace", user.DisplayName);
        Assert.Equal(36, user.Age);
        Assert.True(user.IsActive);
        Assert.Equal(createdUtc, user.CreatedUtc);
        Assert.Null(user.UpdatedUtc);
    }

    [Fact]
    public async Task SingleAsync_WhenRowDoesNotExist_ReturnsNull()
    {
        // Arrange
        await _fixture.ResetAsync();

        var db = _fixture.CreateMooDb();

        var parameters = new MooParams()
            .AddInt("@UserId", 999);

        // Act
        var user = await db.SingleAsync<TestUser>(
            "dbo.usp_User_GetById",
            parameters);

        // Assert
        Assert.Null(user);
    }
}