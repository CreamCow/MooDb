using MooDb.Tests.Integration.Infrastructure.Fixtures;

namespace MooDb.Tests.Integration.Tests.QueryMultiple;

[Collection("MooDb")]
public sealed class QueryMultipleScalarEdgeCaseTests
{
    private readonly MooDbFixture _fixture;

    public QueryMultipleScalarEdgeCaseTests(MooDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task QueryMultipleAsync_WhenScalarResultSetHasNoRows_ReturnsDefaultValue()
    {
        // Arrange
        await _fixture.ResetAsync();

        var db = _fixture.CreateMooDbContext();

        // Act
        var result = await db.Sql.QueryMultipleAsync(
            """
            SELECT CAST(1 AS int) WHERE 1 = 0;
            """,
            read => new ScalarResult
            {
                Value = read.Scalar<int?>()
            });

        // Assert
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task QueryMultipleAsync_WhenScalarResultSetHasMoreThanOneRow_ThrowsInvalidOperationException()
    {
        // Arrange
        await _fixture.ResetAsync();

        var db = _fixture.CreateMooDbContext();

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            db.Sql.QueryMultipleAsync(
                """
                SELECT CAST(1 AS int)
                UNION ALL
                SELECT CAST(2 AS int);
                """,
                read => read.Scalar<int>()));

        // Assert
        Assert.Equal("Expected at most one row but received more than one.", ex.Message);
    }

    private sealed class ScalarResult
    {
        public int? Value { get; init; }
    }
}
