using Microsoft.Data.SqlClient;

namespace MooDb.DependencyInjection;

internal sealed class MooDbContextFactory : IMooDbContextFactory
{
    private readonly MooDbContextFactoryOptions _options;

    public MooDbContextFactory(MooDbContextFactoryOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public global::MooDb.MooDbContext Create(string connectionString)
    {
        MooGuard.AgainstNullOrWhiteSpace(connectionString, nameof(connectionString), "Connection string");
        return new global::MooDb.MooDbContext(connectionString, CreateMooDbOptions());
    }

    public global::MooDb.MooDbContext Create(SqlConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        return new global::MooDb.MooDbContext(connection, CreateMooDbOptions());
    }

    private MooDbContextOptions CreateMooDbOptions()
        => new()
        {
            CommandTimeoutSeconds = _options.CommandTimeoutSeconds,
            StrictAutoMapping = _options.StrictAutoMapping
        };
}