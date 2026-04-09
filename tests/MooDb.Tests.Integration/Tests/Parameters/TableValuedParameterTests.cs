using System.Data;
using MooDb.Tests.Integration.Infrastructure.Fixtures;

namespace MooDb.Tests.Integration.Tests.Parameters;

[Collection("MooDb")]
public sealed class TableValuedParameterTests
{
    private readonly MooDbFixture _fixture;

    public TableValuedParameterTests(MooDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ExecuteAsync_WhenUsingTableValuedParameter_InsertsRows()
    {
        // Arrange
        await _fixture.ResetAsync();

        var table = CreateUserSeedTable();
        table.Rows.Add("ada.lovelace@example.com", "Ada Lovelace", 36, true, new DateTime(2024, 01, 02, 03, 04, 05), DBNull.Value);
        table.Rows.Add("grace.hopper@example.com", "Grace Hopper", 85, true, new DateTime(2024, 02, 03, 04, 05, 06), DBNull.Value);

        var parameters = new MooParams()
            .AddTableValuedParameter("@Users", table, "Tests.udt_UserSeed");

        var db = _fixture.CreateMooDbContext();

        // Act
        var affectedRows = await db.ExecuteAsync("Tests.usp_User_InsertFromTable", parameters);

        // Assert
        Assert.Equal(2, affectedRows);
        Assert.Equal(2, await _fixture.ScalarSqlAsync<int>("SELECT COUNT(*) FROM [dbo].[tbl_User];"));
    }

    [Fact]
    public async Task ExecuteAsync_WhenUsingEmptyTableValuedParameter_InsertsNoRows()
    {
        // Arrange
        await _fixture.ResetAsync();

        var table = CreateUserSeedTable();

        var parameters = new MooParams()
            .AddTableValuedParameter("@Users", table, "Tests.udt_UserSeed");

        var db = _fixture.CreateMooDbContext();

        // Act
        var affectedRows = await db.ExecuteAsync("Tests.usp_User_InsertFromTable", parameters);

        // Assert
        Assert.Equal(0, affectedRows);
        Assert.Equal(0, await _fixture.ScalarSqlAsync<int>("SELECT COUNT(*) FROM [dbo].[tbl_User];"));
    }

    private static DataTable CreateUserSeedTable()
    {
        var table = new DataTable();
        table.Columns.Add("Email", typeof(string));
        table.Columns.Add("DisplayName", typeof(string));
        table.Columns.Add("Age", typeof(int));
        table.Columns.Add("IsActive", typeof(bool));
        table.Columns.Add("CreatedUtc", typeof(DateTime));
        table.Columns.Add("UpdatedUtc", typeof(DateTime));
        return table;
    }
}
