using Microsoft.Data.SqlClient;
using MooDb.Tests.Integration.Infrastructure.Fixtures;

namespace MooDb.Tests.Integration.Tests.Smoke;

[Collection("MooDb")]
public sealed class ExecuteAsyncTests
{
    private readonly MooDbFixture _fixture;

    public ExecuteAsyncTests(MooDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ExecuteAsync_ValidInsert_ReturnsAffectedRowCount()
    {
        // Arrange
        await _fixture.ResetAsync();

        var db = _fixture.CreateMooDbContext();

        var createdUtc = new DateTime(2024, 01, 02, 03, 04, 05, DateTimeKind.Unspecified);

        var parameters = new MooParams()
            .AddNVarChar("@Email", "ada.lovelace@example.com", 320)
            .AddNVarChar("@DisplayName", "Ada Lovelace", 200)
            .AddInt("@Age", 36)
            .AddBit("@IsActive", true)
            .AddDateTime2("@CreatedUtc", createdUtc)
            .AddDateTime2("@UpdatedUtc", null);

        // Act
        var affectedRows = await db.ExecuteAsync("dbo.usp_User_Insert", parameters);

        // Assert
        Assert.Equal(1, affectedRows);

        var userCount = await _fixture.ScalarSqlAsync<int>(
            "SELECT COUNT(*) FROM [dbo].[tbl_User];");

        Assert.Equal(1, userCount);

        var displayName = await _fixture.ScalarSqlAsync<string>(
            "SELECT TOP (1) [DisplayName] FROM [dbo].[tbl_User];");

        Assert.Equal("Ada Lovelace", displayName);
    }

    [Fact]
    public async Task ExecuteAsync_ValidUpdate_ReturnsAffectedRowCount()
    {
        // Arrange
        await _fixture.ResetAsync();

        var createdUtc = new DateTime(2024, 01, 02, 03, 04, 05, DateTimeKind.Unspecified);
        var updatedUtc = new DateTime(2024, 02, 03, 04, 05, 06, DateTimeKind.Unspecified);

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
            (
                @Email,
                @DisplayName,
                @Age,
                @IsActive,
                @CreatedUtc,
                @UpdatedUtc
            );
            """,
            [
                new SqlParameter("@Email", "grace.hopper@example.com"),
                new SqlParameter("@DisplayName", "Grace Hopper"),
                new SqlParameter("@Age", 85),
                new SqlParameter("@IsActive", true),
                new SqlParameter("@CreatedUtc", createdUtc),
                new SqlParameter("@UpdatedUtc", DBNull.Value)
            ]);

        var userId = await _fixture.ScalarSqlAsync<int>(
            "SELECT TOP (1) [UserId] FROM [dbo].[tbl_User];");

        var db = _fixture.CreateMooDbContext();

        var parameters = new MooParams()
            .AddInt("@UserId", userId)
            .AddNVarChar("@DisplayName", "Rear Admiral Grace Hopper", 200)
            .AddDateTime2("@UpdatedUtc", updatedUtc);

        // Act
        var affectedRows = await db.ExecuteAsync("dbo.usp_User_UpdateDisplayName", parameters);

        // Assert
        Assert.Equal(1, affectedRows);

        var displayName = await _fixture.ScalarSqlAsync<string>(
            """
            SELECT TOP (1) [DisplayName]
            FROM [dbo].[tbl_User]
            WHERE [UserId] = @UserId;
            """,
            [
                new SqlParameter("@UserId", userId)
            ]);

        Assert.Equal("Rear Admiral Grace Hopper", displayName);

        var persistedUpdatedUtc = await _fixture.ScalarSqlAsync<DateTime>(
            """
            SELECT TOP (1) [UpdatedUtc]
            FROM [dbo].[tbl_User]
            WHERE [UserId] = @UserId;
            """,
            [
                new SqlParameter("@UserId", userId)
            ]);

        Assert.Equal(updatedUtc, persistedUpdatedUtc);
    }
}
