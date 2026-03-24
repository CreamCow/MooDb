using System.Data;
using Microsoft.Data.SqlClient;
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
        await _fixture.ResetAsync();

        var parameters = new MooParams()
            .AddInt("@InputValue", 41)
            .AddInt("@OutputValue", null, ParameterDirection.Output)
            .AddNVarChar("@InputOutputText", "Start", 100, ParameterDirection.InputOutput);

        var db = _fixture.CreateMooDb();

        await db.ExecuteAsync("Tests.usp_OutputParameters", parameters);

        Assert.Equal(42, parameters.GetInt("@OutputValue"));
        Assert.Equal("Start_Processed", parameters.GetString("@InputOutputText"));
    }

    [Fact]
    public async Task ExecuteAsync_WhenUsingRawSqlParameters_CopiesOutputValuesBackToCallerParameters()
    {
        await _fixture.ResetAsync();

        var output = new SqlParameter("@OutputValue", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };

        var inputOutput = new SqlParameter("@InputOutputText", SqlDbType.NVarChar, 100)
        {
            Direction = ParameterDirection.InputOutput,
            Value = "Raw"
        };

        var parameters = new SqlParameter[]
        {
            new("@InputValue", 9),
            output,
            inputOutput
        };

        var db = _fixture.CreateMooDb();

        await db.ExecuteAsync("Tests.usp_OutputParameters", parameters);

        Assert.Equal(10, (int)output.Value);
        Assert.Equal("Raw_Processed", (string)inputOutput.Value);
    }

    [Fact]
    public async Task ExecuteAsync_WhenInputOutputParameterStartsNull_GetNullableStringReturnsNull()
    {
        await _fixture.ResetAsync();

        var parameters = new MooParams()
            .AddInt("@InputValue", 1)
            .AddInt("@OutputValue", null, ParameterDirection.Output)
            .AddNVarChar("@InputOutputText", null, 100, ParameterDirection.InputOutput);

        var db = _fixture.CreateMooDb();

        await db.ExecuteAsync("Tests.usp_OutputParameters", parameters);

        Assert.Equal(2, parameters.GetInt("@OutputValue"));
        Assert.Null(parameters.GetNullableString("@InputOutputText"));
    }

    [Fact]
    public async Task ExecuteAsync_WhenOutputValueReadAsWrongType_ThrowsInvalidOperationException()
    {
        await _fixture.ResetAsync();

        var parameters = new MooParams()
            .AddInt("@InputValue", 5)
            .AddInt("@OutputValue", null, ParameterDirection.Output)
            .AddNVarChar("@InputOutputText", "Hello", 100, ParameterDirection.InputOutput);

        var db = _fixture.CreateMooDb();

        await db.ExecuteAsync("Tests.usp_OutputParameters", parameters);

        Assert.Throws<InvalidOperationException>(() => parameters.GetString("@OutputValue"));
    }
}
