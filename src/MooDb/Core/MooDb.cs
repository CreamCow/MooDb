using Microsoft.Data.SqlClient;
using MooDb.Configuration;
using MooDb.Execution;
using MooDb.Mapping;
using System.Data;


namespace MooDb.Core
{
    public sealed class MooDb
    {
        private readonly MooCommandExecutor _executor;
        private readonly string? _connectionString;
        private readonly SqlConnection? _connection;
        private readonly MooMapper _mapper;

        public MooDb(string connectionString, MooDbOptions? options = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _connection = null;

            var opts = options ?? new MooDbOptions();
            _executor = new MooCommandExecutor(opts.CommandTimeoutSeconds);
            _mapper = new MooMapper(opts.StrictAutoMapping);
        }

        public MooDb(SqlConnection connection, MooDbOptions? options = null)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _connectionString = null;

            var opts = options ?? new MooDbOptions();
            _executor = new MooCommandExecutor(opts.CommandTimeoutSeconds);
            _mapper = new MooMapper(opts.StrictAutoMapping);
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

        public Task<List<T>> ListAsync<T>(
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
                    await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                    return await _mapper.MapListAsync<T>(reader, cancellationToken);
                },
                cancellationToken);
        }


        public Task<T?> SingleAsync<T>(
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
                    await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

                    var results = await _mapper.MapListAsync<T>(reader, cancellationToken);

                    if (results.Count == 0)
                        return default;

                    if (results.Count > 1)
                        throw new InvalidOperationException(
                            $"Expected at most one row but received {results.Count}.");

                    return results[0];
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