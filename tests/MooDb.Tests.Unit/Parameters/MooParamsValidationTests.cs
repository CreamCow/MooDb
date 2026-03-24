using System.Data;

namespace MooDb.Tests.Unit.Parameters;

public sealed class MooParamsValidationTests
{
    [Fact]
    public void AddDateTime2_WhenScaleIsGreaterThanSeven_ThrowsArgumentOutOfRangeException()
    {
        var parameters = new MooParams();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            parameters.AddDateTime2("@CreatedUtc", DateTime.UtcNow, scale: 8));
    }

    [Fact]
    public void AddDateTimeOffset_WhenScaleIsGreaterThanSeven_ThrowsArgumentOutOfRangeException()
    {
        var parameters = new MooParams();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            parameters.AddDateTimeOffset("@CreatedUtc", DateTimeOffset.UtcNow, scale: 8));
    }

    [Fact]
    public void AddTime_WhenScaleIsGreaterThanSeven_ThrowsArgumentOutOfRangeException()
    {
        var parameters = new MooParams();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            parameters.AddTime("@At", TimeSpan.FromMinutes(5), scale: 8));
    }

    [Fact]
    public void AddDecimal_WhenPrecisionIsZero_ThrowsArgumentOutOfRangeException()
    {
        var parameters = new MooParams();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            parameters.AddDecimal("@Total", 12.34m, precision: 0, scale: 2));
    }

    [Fact]
    public void AddDecimal_WhenScaleIsGreaterThanPrecision_ThrowsArgumentOutOfRangeException()
    {
        var parameters = new MooParams();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            parameters.AddDecimal("@Total", 12.34m, precision: 4, scale: 5));
    }

    [Fact]
    public void AddNVarChar_WhenSizeIsZero_ThrowsArgumentOutOfRangeException()
    {
        var parameters = new MooParams();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            parameters.AddNVarChar("@DisplayName", "Ada", size: 0));
    }

    [Fact]
    public void AddTableValuedParameter_WhenTypeNameIsBlank_ThrowsArgumentException()
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));

        var parameters = new MooParams();

        Assert.Throws<ArgumentException>(() =>
            parameters.AddTableValuedParameter("@Items", table, " "));
    }

    [Fact]
    public void GetInt_WhenParameterDoesNotExist_ThrowsInvalidOperationException()
    {
        var parameters = new MooParams();

        Assert.Throws<InvalidOperationException>(() => parameters.GetInt("@Missing"));
    }

    [Fact]
    public void AddInt_WhenNameIsBlank_ThrowsArgumentException()
    {
        var parameters = new MooParams();

        Assert.Throws<ArgumentException>(() => parameters.AddInt(" ", 1));
    }
}