using MooDb.Tests.Integration.Infrastructure.Fixtures;
using MooDb.Tests.Integration.Infrastructure.Models;

namespace MooDb.Tests.Integration.Tests.Mapping;

[Collection("MooDb")]
public sealed class EnumMappingTests
{
    private readonly MooDbFixture _fixture;

    public EnumMappingTests(MooDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SingleAsync_WhenIntegerColumnMapsToEnum_MapsExpectedEnumValue()
    {
        // Arrange
        await _fixture.ResetAsync();

        var db = _fixture.CreateMooDbContext();

        // Act
        var result = await db.Sql.SingleAsync<IntEnumRow>("SELECT CAST(2 AS INT) AS [Status];");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestStatus.Suspended, result.Status);
    }

    [Fact]
    public async Task SingleAsync_WhenStringColumnMapsToEnum_MapsExpectedEnumValue()
    {
        // Arrange
        await _fixture.ResetAsync();

        var db = _fixture.CreateMooDbContext();

        // Act
        var result = await db.Sql.SingleAsync<StringEnumRow>("SELECT CAST(N'Active' AS NVARCHAR(20)) AS [Status];");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestStatus.Active, result.Status);
    }

    private sealed class IntEnumRow
    {
        public TestStatus Status { get; set; }
    }

    private sealed class StringEnumRow
    {
        public TestStatus Status { get; set; }
    }
}
