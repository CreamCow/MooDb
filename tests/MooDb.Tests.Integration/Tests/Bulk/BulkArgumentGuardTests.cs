using System.Data;

namespace MooDb.Tests.Unit.Bulk;

public sealed class BulkArgumentGuardTests
{
    private const string ConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=Fake;Trusted_Connection=True;";

    [Fact]
    public async Task MooDbBulkWriteToTableAsyncDataTable_WhenTableNameIsBlank_ThrowsArgumentException()
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
    public async Task MooDbBulkWriteToTableAsyncDataTable_WhenDataTableIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var db = new MooDbContext(ConnectionString);

        // Act
        var action = () => db.Bulk.WriteToTableAsync("dbo.tbl_User", (DataTable)null!);

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(action);
    }

    [Fact]
    public async Task MooDbBulkWriteToTableAsyncTyped_WhenTableNameIsBlank_ThrowsArgumentException()
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
    public async Task MooDbBulkWriteToTableAsyncTyped_WhenRowsIsNull_ThrowsArgumentNullException()
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
        public string DisplayName { get; init; } = string.Empty;
        public int? Age { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedUtc { get; init; }
        public DateTime? UpdatedUtc { get; init; }
    }
}