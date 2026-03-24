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
    public async Task SqlScalarAsync_WhenSqlNullReturned_ThrowsInvalidOperationException()
    {
        await _fixture.ResetAsync();

        var db = _fixture.CreateMooDb();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await db.Sql.ScalarAsync<int>("SELECT CAST(NULL AS int);"));
    }

    [Fact]
    public async Task SqlScalarOrDefaultAsync_WhenSqlNullReturned_ReturnsDefault()
    {
        await _fixture.ResetAsync();

        var db = _fixture.CreateMooDb();

        var value = await db.Sql.ScalarOrDefaultAsync<int>("SELECT CAST(NULL AS int);");

        Assert.Equal(0, value);
    }
}
