using System.Data;
using Microsoft.Data.SqlClient;

namespace MooDb.Tests.Unit.Parameters;

public sealed class MooParamsTests
{
    [Fact]
    public void AddInt_WhenCalled_AddsParameterWithExpectedMetadata()
    {
        var parameters = new MooParams()
            .AddInt("@UserId", 42);

        Assert.Single(parameters);
        Assert.Equal("@UserId", parameters[0].ParameterName);
        Assert.Equal(SqlDbType.Int, parameters[0].SqlDbType);
        Assert.Equal(42, parameters[0].Value);
        Assert.Equal(ParameterDirection.Input, parameters[0].Direction);
    }

    [Fact]
    public void AddNVarChar_WhenCalled_AddsParameterWithExpectedSize()
    {
        var parameters = new MooParams()
            .AddNVarChar("@DisplayName", "Ada", 200);

        Assert.Single(parameters);
        Assert.Equal(SqlDbType.NVarChar, parameters[0].SqlDbType);
        Assert.Equal(200, parameters[0].Size);
        Assert.Equal("Ada", parameters[0].Value);
    }

    [Fact]
    public void AddTableValuedParameter_WhenCalled_AddsStructuredParameterWithTypeName()
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));

        var parameters = new MooParams()
            .AddTableValuedParameter("@Items", table, "Tests.udt_Items");

        Assert.Single(parameters);
        Assert.Equal(SqlDbType.Structured, parameters[0].SqlDbType);
        Assert.Equal("Tests.udt_Items", parameters[0].TypeName);
        Assert.Same(table, parameters[0].Value);
        Assert.Equal(ParameterDirection.Input, parameters[0].Direction);
    }

    [Fact]
    public void AddStructured_WhenCalled_DelegatesToTableValuedParameter()
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));

        var parameters = new MooParams()
            .AddStructured("@Items", table, "Tests.udt_Items");

        Assert.Single(parameters);
        Assert.Equal(SqlDbType.Structured, parameters[0].SqlDbType);
        Assert.Equal("Tests.udt_Items", parameters[0].TypeName);
    }

    [Fact]
    public void AddInt_WhenDuplicateNameAdded_ThrowsInvalidOperationException()
    {
        var parameters = new MooParams()
            .AddInt("@UserId", 1);

        Assert.Throws<InvalidOperationException>(() => parameters.AddInt("@UserId", 2));
    }

    [Fact]
    public void GetInt_WhenOutputValuePresent_ReturnsValue()
    {
        var parameters = new MooParams()
            .AddInt("@OutputValue", null, ParameterDirection.Output);

        parameters[0].Value = 123;

        Assert.Equal(123, parameters.GetInt("@OutputValue"));
    }

    [Fact]
    public void GetNullableString_WhenValueIsDBNull_ReturnsNull()
    {
        var parameters = new MooParams()
            .AddNVarChar("@Text", null, 100, ParameterDirection.Output);

        parameters[0].Value = DBNull.Value;

        Assert.Null(parameters.GetNullableString("@Text"));
    }

    [Fact]
    public void GetString_WhenParameterContainsWrongType_ThrowsInvalidOperationException()
    {
        var parameters = new MooParams()
            .AddInt("@OutputValue", null, ParameterDirection.Output);

        parameters[0].Value = 99;

        Assert.Throws<InvalidOperationException>(() => parameters.GetString("@OutputValue"));
    }

    [Fact]
    public void Add_WhenRawParameterIsNull_ThrowsArgumentNullException()
    {
        var parameters = new MooParams();

        Assert.Throws<ArgumentNullException>(() => parameters.Add(null!));
    }

    [Fact]
    public void Enumerator_WhenParametersAdded_EnumeratesInInsertionOrder()
    {
        var parameters = new MooParams()
            .AddInt("@First", 1)
            .AddInt("@Second", 2);

        var names = parameters.Select(p => p.ParameterName).ToArray();

        Assert.Equal(new[] { "@First", "@Second" }, names);
    }
}
