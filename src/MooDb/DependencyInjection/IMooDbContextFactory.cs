using Microsoft.Data.SqlClient;

namespace MooDb.DependencyInjection;

/// <summary>
/// Creates <see cref="global::MooDb.MooDbContext"/> instances for caller-supplied databases.
/// </summary>
/// <remarks>
/// <para>
/// This abstraction is intended for applications where the target database is chosen at runtime,
/// such as SaaS systems with separate admin and tenant databases.
/// </para>
/// <para>
/// The caller resolves the correct connection string or connection, then asks the factory to create
/// a <see cref="global::MooDb.MooDbContext"/> instance for that target.
/// </para>
/// </remarks>
public interface IMooDbContextFactory
{
    /// <summary>
    /// Creates a <see cref="global::MooDb.MooDbContext"/> instance for the supplied SQL Server connection string.
    /// </summary>
    /// <param name="connectionString">The SQL Server connection string.</param>
    /// <returns>A new <see cref="global::MooDb.MooDbContext"/> instance.</returns>
    global::MooDb.MooDbContext Create(string connectionString);

    /// <summary>
    /// Creates a <see cref="global::MooDb.MooDbContext"/> instance for the supplied caller-managed SQL connection.
    /// </summary>
    /// <param name="connection">The SQL Server connection.</param>
    /// <returns>A new <see cref="global::MooDb.MooDbContext"/> instance.</returns>
    global::MooDb.MooDbContext Create(SqlConnection connection);
}