namespace MooDb.DependencyInjection;

/// <summary>
/// Provides default MooDb settings used by <see cref="IMooDbFactory"/> when creating instances.
/// </summary>
public sealed class MooDbFactoryOptions
{
    /// <summary>
    /// Gets or sets the default command timeout, in seconds, applied to created <see cref="global::MooDb.MooDb"/> instances.
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets a value indicating whether strict auto-mapping is enabled by default
    /// for created <see cref="global::MooDb.MooDb"/> instances.
    /// </summary>
    public bool StrictAutoMapping { get; set; }
}