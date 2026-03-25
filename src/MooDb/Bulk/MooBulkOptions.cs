namespace MooDb.Bulk;

/// <summary>
/// Configures a MooDb bulk insert operation.
/// </summary>
public sealed class MooBulkOptions
{
    /// <summary>
    /// Gets or sets the number of rows to send to SQL Server in each batch.
    /// </summary>
    public int? BatchSize { get; set; }

    /// <summary>
    /// Gets or sets the bulk copy timeout, in seconds.
    /// </summary>
    public int? BulkCopyTimeoutSeconds { get; set; }

    /// <summary>
    /// Gets or sets SQL to run before the bulk insert starts.
    /// </summary>
    public string? PreparationSql { get; set; }

    /// <summary>
    /// Gets or sets SQL to run after the bulk insert succeeds.
    /// </summary>
    public string? CleanupSql { get; set; }
}