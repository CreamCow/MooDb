using Microsoft.Data.SqlClient;

namespace MooDb.DependencyInjection;

internal sealed class MooDbFactory : IMooDbFactory
{
    private readonly MooDbFactoryOptions _options;

    public MooDbFactory(MooDbFactoryOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public global::MooDb.MooDb Create(string connectionString)
    {
        MooGuard.AgainstNullOrWhiteSpace(connectionString, nameof(connectionString), "Connection string");
        return new global::MooDb.MooDb(connectionString, CreateMooDbOptions());
    }

    public global::MooDb.MooDb Create(SqlConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        return new global::MooDb.MooDb(connection, CreateMooDbOptions());
    }

    private MooDbOptions CreateMooDbOptions()
        => new()
        {
            CommandTimeoutSeconds = _options.CommandTimeoutSeconds,
            StrictAutoMapping = _options.StrictAutoMapping
        };
}