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
        await _fixture.ResetAsync();

        throw new NotImplementedException();
    }

    [Fact]
    public async Task ExecuteAsync_ValidUpdate_ReturnsAffectedRowCount()
    {
        await _fixture.ResetAsync();

        throw new NotImplementedException();
    }
}