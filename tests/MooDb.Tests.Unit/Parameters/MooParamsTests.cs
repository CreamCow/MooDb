using System.Data;
using Microsoft.Data.SqlClient;

namespace MooDb.Tests.Unit.Parameters;

public sealed class MooParamsTests
{
    [Fact]
    public void AddInt_WhenCalled_AddsParameterWithExpectedMetadata()
    {
        // Arrange
        var parameters = new MooParams()
            .AddInt("@UserId", 42);

        // Act

        // Assert
        Assert.Single(parameters);
        Assert.Equal("@UserId", parameters[0].ParameterName);
        Assert.Equal(SqlDbType.Int, parameters[0].SqlDbType);
        Assert.Equal(42, parameters[0].Value);
        Assert.Equal(ParameterDirection.Input, parameters[0].Direction);
    }

    [Fact]
    public void AddNVarChar_WhenCalled_AddsParameterWithExpectedSize()
    {
        // Arrange
        var parameters = new MooParams()
            .AddNVarChar("@DisplayName", "Ada", 200);

        // Act

        // Assert
        Assert.Single(parameters);
        Assert.Equal(SqlDbType.NVarChar, parameters[0].SqlDbType);
        Assert.Equal(200, parameters[0].Size);
        Assert.Equal("Ada", parameters[0].Value);
    }

    [Fact]
    public void AddTableValuedParameter_WhenCalled_AddsStructuredParameterWithTypeName()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));

        var parameters = new MooParams()
            .AddTableValuedParameter("@Items", table, "Tests.udt_Items");

        // Act

        // Assert
        Assert.Single(parameters);
        Assert.Equal(SqlDbType.Structured, parameters[0].SqlDbType);
        Assert.Equal("Tests.udt_Items", parameters[0].TypeName);
        Assert.Same(table, parameters[0].Value);
        Assert.Equal(ParameterDirection.Input, parameters[0].Direction);
    }

    [Fact]
    public void AddInt_WhenDuplicateNameAdded_ThrowsInvalidOperationException()
    {
        // Arrange
        var parameters = new MooParams()
            .AddInt("@UserId", 1);

        // Act
        var action = () => parameters.AddInt("@UserId", 2);

        // Assert
        Assert.Throws<InvalidOperationException>(action);
    }

    [Fact]
    public void GetInt_WhenOutputValuePresent_ReturnsValue()
    {
        // Arrange
        var parameters = new MooParams()
            .AddInt("@OutputValue", null, ParameterDirection.Output);

        parameters[0].Value = 123;

        // Act
        var value = parameters.GetInt("@OutputValue");

        // Assert
        Assert.Equal(123, value);
    }

    [Fact]
    public void GetNullableString_WhenValueIsDBNull_ReturnsNull()
    {
        // Arrange
        var parameters = new MooParams()
            .AddNVarChar("@Text", null, 100, ParameterDirection.Output);

        parameters[0].Value = DBNull.Value;

        // Act
        var value = parameters.GetNullableString("@Text");

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public void GetString_WhenParameterContainsWrongType_ThrowsInvalidOperationException()
    {
        // Arrange
        var parameters = new MooParams()
            .AddInt("@OutputValue", null, ParameterDirection.Output);

        parameters[0].Value = 99;

        // Act
        var action = () => parameters.GetString("@OutputValue");

        // Assert
        Assert.Throws<InvalidOperationException>(action);
    }

    [Fact]
    public void Add_WhenRawParameterIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var parameters = new MooParams();

        // Act
        var action = () => parameters.Add(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Enumerator_WhenParametersAdded_EnumeratesInInsertionOrder()
    {
        // Arrange
        var parameters = new MooParams()
            .AddInt("@First", 1)
            .AddInt("@Second", 2);

        // Act
        var names = parameters.Select(p => p.ParameterName).ToArray();

        // Assert
        Assert.Equal(new[] { "@First", "@Second" }, names);
    }
}
