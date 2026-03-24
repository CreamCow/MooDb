using MooDb.Tests.Integration.Infrastructure.Fixtures;

namespace MooDb.Tests.Integration.Tests.Mapping;

[Collection("MooDb")]
public sealed class ListAsyncCustomMappingTests
{
    private readonly MooDbFixture _fixture;

    public ListAsyncCustomMappingTests(MooDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ListAsync_WhenCustomMapperSupplied_ReturnsMappedProjectionList()
    {
        await _fixture.ResetAsync();

        await _fixture.ExecuteSqlAsync(
            """
            SET IDENTITY_INSERT [dbo].[tbl_User] ON;
            INSERT INTO [dbo].[tbl_User] ([UserId], [Email], [DisplayName], [Age], [IsActive], [CreatedUtc], [UpdatedUtc])
            VALUES
                (2, N'grace@example.com', N'Grace Hopper', 85, 1, '2024-02-03T04:05:06', NULL),
                (1, N'ada@example.com', N'Ada Lovelace', 36, 1, '2024-01-02T03:04:05', NULL);
            SET IDENTITY_INSERT [dbo].[tbl_User] OFF;
            """);

        var db = _fixture.CreateMooDb();

        var users = await db.ListAsync(
            "dbo.usp_User_List",
            static reader => new UserProjection(
                reader.GetInt32(reader.GetOrdinal("UserId")),
                reader.GetString(reader.GetOrdinal("DisplayName"))));

        Assert.Equal(2, users.Count);
        Assert.Equal(1, users[0].UserId);
        Assert.Equal("Ada Lovelace", users[0].DisplayName);
        Assert.Equal(2, users[1].UserId);
        Assert.Equal("Grace Hopper", users[1].DisplayName);
    }

    private sealed record UserProjection(int UserId, string DisplayName);
}
