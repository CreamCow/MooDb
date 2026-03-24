using MooDb.Tests.Integration.Infrastructure.Fixtures;

namespace MooDb.Tests.Integration.Tests.Smoke;

[Collection("MooDb")]
public sealed class SqlScalarAsyncTests
{
    private readonly MooDbFixture _fixture;

    public SqlScalarAsyncTests(MooDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SqlScalarAsync_WhenSqlNullReturnedAndTypeIsNonNullable_ReturnsDefaultValue()
    {
        await _fixture.ResetAsync();

        var db = _fixture.CreateMooDb();

        var value = await db.Sql.ScalarAsync<int>("SELECT CAST(NULL AS int);");

        Assert.Equal(0, value);
    }

    [Fact]
    public async Task SqlScalarAsync_WhenSqlNullReturnedAndTypeIsNullable_ReturnsNull()
    {
        await _fixture.ResetAsync();

        var db = _fixture.CreateMooDb();

        var value = await db.Sql.ScalarAsync<int?>("SELECT CAST(NULL AS int);");

        Assert.Null(value);
    }
}
