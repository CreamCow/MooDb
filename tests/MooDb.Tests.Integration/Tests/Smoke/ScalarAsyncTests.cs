using Microsoft.Data.SqlClient;
using MooDb.Tests.Integration.Infrastructure.Fixtures;

namespace MooDb.Tests.Integration.Tests.Smoke;

[Collection("MooDb")]
public sealed class ScalarAsyncTests
{
    private readonly MooDbFixture _fixture;

    public ScalarAsyncTests(MooDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ScalarAsync_WhenNoRowsExist_ReturnsZero()
    {
        // Arrange
        await _fixture.ResetAsync();

        var db = _fixture.CreateMooDb();

        // Act
        var count = await db.ScalarAsync<int>("dbo.usp_User_Count");

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task ScalarAsync_WhenRowsExist_ReturnsUserCount()
    {
        // Arrange
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
            (
                @Email1,
                @DisplayName1,
                @Age1,
                @IsActive1,
                @CreatedUtc1,
                @UpdatedUtc1
            ),
            (
                @Email2,
                @DisplayName2,
                @Age2,
                @IsActive2,
                @CreatedUtc2,
                @UpdatedUtc2
            );
            """,
            [
                new SqlParameter("@Email1", "ada.lovelace@example.com"),
                new SqlParameter("@DisplayName1", "Ada Lovelace"),
                new SqlParameter("@Age1", 36),
                new SqlParameter("@IsActive1", true),
                new SqlParameter("@CreatedUtc1", new DateTime(2024, 01, 02, 03, 04, 05)),
                new SqlParameter("@UpdatedUtc1", DBNull.Value),

                new SqlParameter("@Email2", "grace.hopper@example.com"),
                new SqlParameter("@DisplayName2", "Grace Hopper"),
                new SqlParameter("@Age2", 85),
                new SqlParameter("@IsActive2", true),
                new SqlParameter("@CreatedUtc2", new DateTime(2024, 02, 03, 04, 05, 06)),
                new SqlParameter("@UpdatedUtc2", DBNull.Value)
            ]);

        var db = _fixture.CreateMooDb();

        // Act
        var count = await db.ScalarAsync<int>("dbo.usp_User_Count");

        // Assert
        Assert.Equal(2, count);
    }
}
