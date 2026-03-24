using MooDb.Tests.Integration.Infrastructure.Fixtures;

namespace MooDb.Tests.Integration.Tests.Mapping;

[Collection("MooDb")]
public sealed class SingleAsyncCustomMappingTests
{
    private readonly MooDbFixture _fixture;

    public SingleAsyncCustomMappingTests(MooDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SingleAsync_WhenCustomMapperSupplied_ReturnsMappedProjection()
    {
        // Arrange
        await _fixture.ResetAsync();

        await _fixture.ExecuteSqlAsync(
            """
            SET IDENTITY_INSERT [dbo].[tbl_User] ON;
            INSERT INTO [dbo].[tbl_User] ([UserId], [Email], [DisplayName], [Age], [IsActive], [CreatedUtc], [UpdatedUtc])
            VALUES (1, N'ada@example.com', N'Ada Lovelace', 36, 1, '2024-01-02T03:04:05', NULL);
            SET IDENTITY_INSERT [dbo].[tbl_User] OFF;
            """);

        var db = _fixture.CreateMooDb();
        var parameters = new MooParams().AddInt("@UserId", 1);

        // Act
        var user = await db.SingleAsync(
            "dbo.usp_User_GetById",
            static reader => new UserProjection(
                reader.GetInt32(reader.GetOrdinal("UserId")),
                reader.GetString(reader.GetOrdinal("DisplayName"))),
            parameters);

        // Assert
        Assert.NotNull(user);
        Assert.Equal(1, user!.UserId);
        Assert.Equal("Ada Lovelace", user.DisplayName);
    }

    private sealed record UserProjection(int UserId, string DisplayName);
}
