using System.Data;
using Microsoft.Data.SqlClient;
using MooDb.Bulk;
using MooDb.Tests.Integration.Infrastructure.Fixtures;

namespace MooDb.Tests.Integration.Tests.Bulk;

[Collection("MooDb")]
public sealed class BulkWriteToTableAsyncTests
{
    private readonly MooDbFixture _fixture;

    public BulkWriteToTableAsyncTests(MooDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task WriteToTableAsync_DataTable_InsertsRows()
    {
        // Arrange
        await _fixture.ResetAsync();

        var db = _fixture.CreateMooDb();
        var createdUtc = new DateTime(2024, 01, 02, 03, 04, 05);

        var table = new DataTable();
        table.Columns.Add("Email", typeof(string));
        table.Columns.Add("DisplayName", typeof(string));
        table.Columns.Add("Age", typeof(int));
        table.Columns.Add("IsActive", typeof(bool));
        table.Columns.Add("CreatedUtc", typeof(DateTime));
        table.Columns.Add("UpdatedUtc", typeof(DateTime));

        table.Rows.Add("ada.lovelace@example.com", "Ada Lovelace", 36, true, createdUtc, DBNull.Value);
        table.Rows.Add("grace.hopper@example.com", "Grace Hopper", 85, true, createdUtc, DBNull.Value);

        // Act
        await db.Bulk.WriteToTableAsync("dbo.tbl_User", table);

        // Assert
        var userCount = await _fixture.ScalarSqlAsync<int>(
            "SELECT COUNT(*) FROM [dbo].[tbl_User];");

        Assert.Equal(2, userCount);
    }

    [Fact]
    public async Task WriteToTableAsync_Typed_InsertsRows()
    {
        // Arrange
        await _fixture.ResetAsync();

        var db = _fixture.CreateMooDb();
        var createdUtc = new DateTime(2024, 01, 02, 03, 04, 05);

        var rows = new[]
        {
            new BulkUserRow
            {
                Email = "katherine.johnson@example.com",
                DisplayName = "Katherine Johnson",
                Age = 50,
                IsActive = true,
                CreatedUtc = createdUtc,
                UpdatedUtc = null
            },
            new BulkUserRow
            {
                Email = "dorothy.vaughan@example.com",
                DisplayName = "Dorothy Vaughan",
                Age = 49,
                IsActive = true,
                CreatedUtc = createdUtc,
                UpdatedUtc = null
            }
        };

        // Act
        await db.Bulk.WriteToTableAsync("dbo.tbl_User", rows);

        // Assert
        var userCount = await _fixture.ScalarSqlAsync<int>(
            "SELECT COUNT(*) FROM [dbo].[tbl_User];");

        Assert.Equal(2, userCount);
    }

    [Fact]
    public async Task WriteToTableAsync_WithPreparationAndCleanupSql_AppliesBoth()
    {
        // Arrange
        await _fixture.ResetAsync();

        await _fixture.ExecuteSqlAsync(
            """
            INSERT INTO [dbo].[tbl_User]
            (
                [Email],
                [DisplayName],
                [Age],
                [IsActive],
                [CreatedUtc],
                [UpdatedUtc]
            )
            VALUES
            (
                @Email,
                @DisplayName,
                @Age,
                @IsActive,
                @CreatedUtc,
                @UpdatedUtc
            );
            """,
            [
                new SqlParameter("@Email", "existing.user@example.com"),
                new SqlParameter("@DisplayName", "Existing User"),
                new SqlParameter("@Age", 30),
                new SqlParameter("@IsActive", true),
                new SqlParameter("@CreatedUtc", new DateTime(2024, 01, 01, 00, 00, 00)),
                new SqlParameter("@UpdatedUtc", DBNull.Value)
            ]);

        var db = _fixture.CreateMooDb();
        var createdUtc = new DateTime(2024, 01, 02, 03, 04, 05);

        var rows = new[]
        {
            new BulkUserRow
            {
                Email = "new.user@example.com",
                DisplayName = "New User",
                Age = 40,
                IsActive = true,
                CreatedUtc = createdUtc,
                UpdatedUtc = null
            }
        };

        var options = new MooBulkOptions
        {
            PreparationSql = "DELETE FROM [dbo].[tbl_User];",
            CleanupSql = """
                         UPDATE [dbo].[tbl_User]
                         SET [DisplayName] = [DisplayName] + N' Imported';
                         """
        };

        // Act
        await db.Bulk.WriteToTableAsync("dbo.tbl_User", rows, options);

        // Assert
        var userCount = await _fixture.ScalarSqlAsync<int>(
            "SELECT COUNT(*) FROM [dbo].[tbl_User];");

        Assert.Equal(1, userCount);

        var displayName = await _fixture.ScalarSqlAsync<string>(
            "SELECT TOP (1) [DisplayName] FROM [dbo].[tbl_User];");

        Assert.Equal("New User Imported", displayName);
    }

    [Fact]
    public async Task WriteToTableAsync_WithinTransaction_RollsBackWhenNotCommitted()
    {
        // Arrange
        await _fixture.ResetAsync();

        var db = _fixture.CreateMooDb();
        var createdUtc = new DateTime(2024, 01, 02, 03, 04, 05);

        var rows = new[]
        {
            new BulkUserRow
            {
                Email = "rollback.user@example.com",
                DisplayName = "Rollback User",
                Age = 25,
                IsActive = true,
                CreatedUtc = createdUtc,
                UpdatedUtc = null
            }
        };

        // Act
        await using (var transaction = await db.BeginTransactionAsync())
        {
            await transaction.Bulk.WriteToTableAsync("dbo.tbl_User", rows);
        }

        // Assert
        var userCount = await _fixture.ScalarSqlAsync<int>(
            "SELECT COUNT(*) FROM [dbo].[tbl_User];");

        Assert.Equal(0, userCount);
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