namespace MooDb.DependencyInjection;

/// <summary>
/// Provides default MooDbContext settings used by <see cref="IMooDbContextFactory"/> when creating instances.
/// </summary>
public sealed class MooDbContextFactoryOptions
{
    /// <summary>
    /// Gets or sets the default command timeout, in seconds, applied to created <see cref="global::MooDb.MooDbContext"/> instances.
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets a value indicating whether strict auto-mapping is enabled by default
    /// for created <see cref="global::MooDb.MooDbContext"/> instances.
    /// </summary>
    public bool StrictAutoMapping { get; set; }
}