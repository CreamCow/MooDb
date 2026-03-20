using System.Data;
using Microsoft.Data.SqlClient;
using MooDb.Configuration;
using MooDb.Execution;


namespace MooDb.Core
{
    public sealed class MooDb
    {
        private readonly MooCommandExecutor _executor;
        private readonly string? _connectionString;
        private readonly SqlConnection? _connection;

        public MooDb(string connectionString, MooDbOptions? options = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _connection = null;

            var opts = options ?? new MooDbOptions();
            _executor = new MooCommandExecutor(opts.CommandTimeoutSeconds);
        }

        public MooDb(SqlConnection connection, MooDbOptions? options = null)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _connectionString = null;

            var opts = options ?? new MooDbOptions();
            _executor = new MooCommandExecutor(opts.CommandTimeoutSeconds);
        }

        public Task<int> ExecuteAsync(
            string procedure,
            IReadOnlyList<SqlParameter>? parameters = null,
            int? commandTimeoutSeconds = null,
            CancellationToken cancellationToken = default)
        {
            var context = CreateExecutionContext();

            return _executor.ExecuteAsync(
                context,
                procedure,
                CommandType.StoredProcedure,
                parameters,
                commandTimeoutSeconds,
                cmd => cmd.ExecuteNonQueryAsync(cancellationToken),
                cancellationToken);
        }



        public Task<T?> ScalarAsync<T>(
            string procedure,
            IReadOnlyList<SqlParameter>? parameters = null,
            int? commandTimeoutSeconds = null,
            CancellationToken cancellationToken = default)
        {
            var context = CreateExecutionContext();

            return _executor.ExecuteAsync(
                context,
                procedure,
                CommandType.StoredProcedure,
                parameters,
                commandTimeoutSeconds,
                async cmd =>
                {
                    var result = await cmd.ExecuteScalarAsync(cancellationToken);

                    if (result is null || result is DBNull)
                        return default;

                    return (T)Convert.ChangeType(result, typeof(T));
                },
                cancellationToken);
        }


        private MooExecutionContext CreateExecutionContext()
        {
            if (_connection is not null)
            {
                return new MooExecutionContext(
                    _connection,
                    transaction: null,
                    ownsConnection: false);
            }

            var connection = new SqlConnection(_connectionString!);

            return new MooExecutionContext(
                connection,
                transaction: null,
                ownsConnection: true);
        }
    }
}