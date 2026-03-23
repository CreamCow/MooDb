namespace MooDb;

/// <summary>
/// Configures MooDb execution and mapping behaviour.
/// </summary>
/// <remarks>
/// Use <see cref="CommandTimeoutSeconds"/> to set the default command timeout and
/// <see cref="StrictAutoMapping"/> to control auto-mapping validation behaviour.
/// </remarks>
public sealed class MooDbOptions
{
    /// <summary>
    /// Gets or sets the default command timeout, in seconds, used when a command-specific timeout is not supplied.
    /// </summary>
    /// <remarks>
    /// The default value is <c>30</c>. Specify <c>0</c> to use no timeout.
    /// </remarks>
    public int CommandTimeoutSeconds { get; set; } = 30;


    /// <summary>
    /// Enables strict validation of result set shape and type compatibility during auto-mapping.
    /// </summary>
    /// <remarks>
    /// When enabled, MooDb will throw an exception if:
    /// - a column in the result set does not have a matching property on the target type
    /// - a property on the target type does not have a corresponding column in the result set
    /// - a value cannot be assigned to the target property type (including failed type conversion)
    ///
    /// By default, auto-mapping is permissive:
    /// - extra columns are ignored
    /// - missing properties are left as their default values
    /// - compatible type conversions are applied where possible (e.g. int to long)
    ///
    /// This setting applies only to auto-mapping. Explicit mapper functions are unaffected.
    /// </remarks>
    public bool StrictAutoMapping { get; set; }
}