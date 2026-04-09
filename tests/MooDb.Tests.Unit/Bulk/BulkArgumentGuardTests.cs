using System.Data;

namespace MooDb.Tests.Unit.Bulk;

public sealed class BulkArgumentGuardTests
{
    private const string ConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=Fake;Trusted_Connection=True;";

    [Fact]
    public async Task WriteToTableAsync_DataTable_WhenTableNameIsBlank_ThrowsArgumentException()
    {
        // Arrange
        var db = new MooDbContext(ConnectionString);
        var table = new DataTable();

        // Act
        var action = () => db.Bulk.WriteToTableAsync(" ", table);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task WriteToTableAsync_DataTable_WhenDataTableIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var db = new MooDbContext(ConnectionString);

        // Act
        var action = () => db.Bulk.WriteToTableAsync("dbo.tbl_User", (DataTable)null!);

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(action);
    }

    [Fact]
    public async Task WriteToTableAsync_Typed_WhenTableNameIsBlank_ThrowsArgumentException()
    {
        // Arrange
        var db = new MooDbContext(ConnectionString);
        var rows = new[] { new BulkUserRow() };

        // Act
        var action = () => db.Bulk.WriteToTableAsync(" ", rows);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public async Task WriteToTableAsync_Typed_WhenRowsIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var db = new MooDbContext(ConnectionString);

        // Act
        var action = () => db.Bulk.WriteToTableAsync<BulkUserRow>("dbo.tbl_User", null!);

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(action);
    }

    private sealed class BulkUserRow
    {
        public string Email { get; init; } = string.Empty;
    }
}