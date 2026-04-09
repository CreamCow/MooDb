using System.Data;
using MooDb.Tests.Integration.Infrastructure.Fixtures;

namespace MooDb.Tests.Integration.Tests.Parameters;

[Collection("MooDb")]
public sealed class ExecuteAsyncOutputParameterTests
{
    private readonly MooDbFixture _fixture;

    public ExecuteAsyncOutputParameterTests(MooDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ExecuteAsync_WhenUsingMooParams_SetsOutputAndInputOutputValues()
    {
        // Arrange
        await _fixture.ResetAsync();

        var parameters = new MooParams()
            .AddInt("@InputValue", 41)
            .AddInt("@OutputValue", null, ParameterDirection.Output)
            .AddNVarChar("@InputOutputText", "Start", 100, ParameterDirection.InputOutput);

        var db = _fixture.CreateMooDbContext();

        // Act
        await db.ExecuteAsync("Tests.usp_OutputParameters", parameters);

        // Assert
        Assert.Equal(42, parameters.GetInt("@OutputValue"));
        Assert.Equal("Start_Processed", parameters.GetString("@InputOutputText"));
    }

    [Fact]
    public async Task ExecuteAsync_WhenUsingMooParams_CopiesOutputValuesBackToCallerParameters()
    {
        // Arrange
        await _fixture.ResetAsync();

        var parameters = new MooParams()
            .AddInt("@InputValue", 9)
            .AddInt("@OutputValue", null, ParameterDirection.Output)
            .AddNVarChar("@InputOutputText", "Raw", 100, ParameterDirection.InputOutput);

        var db = _fixture.CreateMooDbContext();

        // Act
        await db.ExecuteAsync("Tests.usp_OutputParameters", parameters);

        // Assert
        Assert.Equal(10, parameters.GetInt("@OutputValue"));
        Assert.Equal("Raw_Processed", parameters.GetString("@InputOutputText"));
    }

    [Fact]
    public async Task ExecuteAsync_WhenInputOutputParameterStartsNull_GetNullableStringReturnsNull()
    {
        // Arrange
        await _fixture.ResetAsync();

        var parameters = new MooParams()
            .AddInt("@InputValue", 1)
            .AddInt("@OutputValue", null, ParameterDirection.Output)
            .AddNVarChar("@InputOutputText", null, 100, ParameterDirection.InputOutput);

        var db = _fixture.CreateMooDbContext();

        // Act
        await db.ExecuteAsync("Tests.usp_OutputParameters", parameters);

        // Assert
        Assert.Equal(2, parameters.GetInt("@OutputValue"));
        Assert.Null(parameters.GetNullableString("@InputOutputText"));
    }

    [Fact]
    public async Task ExecuteAsync_WhenOutputValueReadAsWrongType_ThrowsInvalidOperationException()
    {
        // Arrange
        await _fixture.ResetAsync();

        var parameters = new MooParams()
            .AddInt("@InputValue", 5)
            .AddInt("@OutputValue", null, ParameterDirection.Output)
            .AddNVarChar("@InputOutputText", "Hello", 100, ParameterDirection.InputOutput);

        var db = _fixture.CreateMooDbContext();

        // Act
        await db.ExecuteAsync("Tests.usp_OutputParameters", parameters);

        // Assert
        Assert.Throws<InvalidOperationException>(() => parameters.GetString("@OutputValue"));
    }

    [Fact]
    public async Task ExecuteAsync_WhenOutputParameterIsDbNull_AllowsNullableGettersToReturnNull()
    {
        // Arrange
        await _fixture.ResetAsync();

        var parameters = new MooParams()
            .AddInt("@OutputValue", null, ParameterDirection.Output)
            .AddNVarChar("@InputOutputText", "Seed", 100, ParameterDirection.InputOutput);

        var db = _fixture.CreateMooDbContext();

        // Act
        await db.ExecuteAsync("Tests.usp_OutputParameters_DbNull", parameters);

        // Assert
        Assert.Null(parameters.GetNullableInt("@OutputValue"));
        Assert.Null(parameters.GetNullableString("@InputOutputText"));
    }
}
