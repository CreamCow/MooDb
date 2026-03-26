using MooDb.Tests.Integration.Infrastructure.Fixtures;
using MooDb.Tests.Integration.Infrastructure.Models;

namespace MooDb.Tests.Integration.Tests.Smoke;

[Collection("MooDb")]
public sealed class SqlScalarEnumTests
{
    private readonly MooDbFixture _fixture;

    public SqlScalarEnumTests(MooDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ScalarAsync_WhenIntegerResultMapsToEnum_ReturnsExpectedEnumValue()
    {
        // Arrange
        await _fixture.ResetAsync();

        var db = _fixture.CreateMooDb();

        // Act
        var result = await db.Sql.ScalarAsync<TestStatus>("SELECT CAST(1 AS INT);");

        // Assert
        Assert.Equal(TestStatus.Active, result);
    }

    [Fact]
    public async Task ScalarAsync_WhenStringResultMapsToEnum_ReturnsExpectedEnumValue()
    {
        // Arrange
        await _fixture.ResetAsync();

        var db = _fixture.CreateMooDb();

        // Act
        var result = await db.Sql.ScalarAsync<TestStatus>("SELECT CAST(N'Suspended' AS NVARCHAR(20));");

        // Assert
        Assert.Equal(TestStatus.Suspended, result);
    }
}
