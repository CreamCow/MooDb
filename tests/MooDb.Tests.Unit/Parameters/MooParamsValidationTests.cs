using System.Data;

namespace MooDb.Tests.Unit.Parameters;

public sealed class MooParamsValidationTests
{
    [Fact]
    public void AddDateTime2_WhenScaleIsGreaterThanSeven_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var parameters = new MooParams();

        // Act
        var action = () => parameters.AddDateTime2("@CreatedUtc", DateTime.UtcNow, scale: 8);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void AddDateTimeOffset_WhenScaleIsGreaterThanSeven_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var parameters = new MooParams();

        // Act
        var action = () => parameters.AddDateTimeOffset("@CreatedUtc", DateTimeOffset.UtcNow, scale: 8);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void AddTime_WhenScaleIsGreaterThanSeven_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var parameters = new MooParams();

        // Act
        var action = () => parameters.AddTime("@At", TimeSpan.FromMinutes(5), scale: 8);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void AddDecimal_WhenPrecisionIsZero_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var parameters = new MooParams();

        // Act
        var action = () => parameters.AddDecimal("@Total", 12.34m, precision: 0, scale: 2);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void AddDecimal_WhenScaleIsGreaterThanPrecision_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var parameters = new MooParams();

        // Act
        var action = () => parameters.AddDecimal("@Total", 12.34m, precision: 4, scale: 5);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void AddNVarChar_WhenSizeIsZero_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var parameters = new MooParams();

        // Act
        var action = () => parameters.AddNVarChar("@DisplayName", "Ada", size: 0);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void AddTableValuedParameter_WhenTypeNameIsBlank_ThrowsArgumentException()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));

        var parameters = new MooParams();

        // Act
        var action = () => parameters.AddTableValuedParameter("@Items", table, " ");

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void GetInt_WhenParameterDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange
        var parameters = new MooParams();

        // Act
        Func<int> action = () => parameters.GetInt("@Missing");

        // Assert
        Assert.Throws<InvalidOperationException>(() => action());
    }

    [Fact]
    public void AddInt_WhenNameIsBlank_ThrowsArgumentException()
    {
        // Arrange
        var parameters = new MooParams();

        // Act
        var action = () => parameters.AddInt(" ", 1);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }


    [Fact]
    public void Add_WhenRawParameterNameIsBlank_ThrowsArgumentException()
    {
        // Arrange
        var parameters = new MooParams();

        // Act
        var action = () => parameters.Add(new Microsoft.Data.SqlClient.SqlParameter());

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void AddTableValuedParameter_WhenValueIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var parameters = new MooParams();

        // Act
        var action = () => parameters.AddTableValuedParameter("@Items", null!, "Tests.udt_Items");

        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }
}
