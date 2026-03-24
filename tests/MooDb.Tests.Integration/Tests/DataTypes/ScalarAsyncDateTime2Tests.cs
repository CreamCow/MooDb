using MooDb.Tests.Integration.Infrastructure.Fixtures;

namespace MooDb.Tests.Integration.Tests.DataTypes;

[Collection("MooDb")]
public sealed class ScalarAsyncDateTime2Tests
{
    private readonly MooDbFixture _fixture;

    public ScalarAsyncDateTime2Tests(MooDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ScalarAsync_WhenDateTime2MinValueRoundTrips_ReturnsExpectedValue()
    {
        await _fixture.ResetAsync();

        var expected = DateTime.MinValue;
        var parameters = new MooParams().AddDateTime2("@Value", expected, scale: 7);
        var db = _fixture.CreateMooDb();

        var actual = await db.ScalarAsync<DateTime>("Tests.usp_DateTime2_RoundTrip", parameters);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task ScalarAsync_WhenDateTime2MaxValueRoundTrips_ReturnsExpectedValue()
    {
        await _fixture.ResetAsync();

        var expected = DateTime.MaxValue;
        var parameters = new MooParams().AddDateTime2("@Value", expected, scale: 7);
        var db = _fixture.CreateMooDb();

        var actual = await db.ScalarAsync<DateTime>("Tests.usp_DateTime2_RoundTrip", parameters);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task ScalarAsync_WhenDateTime2HasSevenDigitPrecision_PreservesPrecision()
    {
        await _fixture.ResetAsync();

        var expected = new DateTime(2024, 01, 02, 03, 04, 05, 123).AddTicks(4567);
        var parameters = new MooParams().AddDateTime2("@Value", expected, scale: 7);
        var db = _fixture.CreateMooDb();

        var actual = await db.ScalarAsync<DateTime>("Tests.usp_DateTime2_RoundTrip", parameters);

        Assert.Equal(expected, actual);
        Assert.Equal(expected.Ticks, actual.Ticks);
    }
}
