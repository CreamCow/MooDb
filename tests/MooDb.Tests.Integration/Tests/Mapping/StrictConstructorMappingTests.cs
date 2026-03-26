using MooDb.Tests.Integration.Infrastructure.Fixtures;

namespace MooDb.Tests.Integration.Tests.Mapping;

[Collection("MooDb")]
public sealed class StrictConstructorMappingTests
{
    private readonly MooDbFixture _fixture;

    public StrictConstructorMappingTests(MooDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SingleAsync_WhenStrictConstructorAndWritablePropertyMatch_ComposesObjectSuccessfully()
    {
        // Arrange
        await _fixture.ResetAsync();

        var db = _fixture.CreateStrictMooDb();

        // Act
        var result = await db.Sql.SingleAsync<ConstructorAndPropertyRow>(
            "SELECT 1 AS [UserId], CAST(N'ada@example.com' AS NVARCHAR(320)) AS [Email], CAST(N'Ada' AS NVARCHAR(200)) AS [DisplayName];");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.UserId);
        Assert.Equal("ada@example.com", result.Email);
        Assert.Equal("Ada", result.DisplayName);
    }

    [Fact]
    public async Task SingleAsync_WhenStrictConstructorLeavesWritablePropertyUnmapped_ThrowsInvalidOperationException()
    {
        // Arrange
        await _fixture.ResetAsync();

        var db = _fixture.CreateStrictMooDb();

        // Act
        var action = () => db.Sql.SingleAsync<MissingPropertyRow>(
            "SELECT 1 AS [UserId], CAST(N'ada@example.com' AS NVARCHAR(320)) AS [Email];");

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(action);
    }

    private sealed class ConstructorAndPropertyRow
    {
        public ConstructorAndPropertyRow(int userId, string email)
        {
            UserId = userId;
            Email = email;
        }

        public int UserId { get; }
        public string Email { get; }
        public string DisplayName { get; set; } = string.Empty;
    }

    private sealed class MissingPropertyRow
    {
        public MissingPropertyRow(int userId, string email)
        {
            UserId = userId;
            Email = email;
        }

        public int UserId { get; }
        public string Email { get; }
        public string DisplayName { get; set; } = string.Empty;
    }
}
