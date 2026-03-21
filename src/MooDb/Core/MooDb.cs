using Microsoft.Data.SqlClient;
using MooDb.Configuration;
using MooDb.Execution;
using MooDb.Mapping;
using MooDb.Sql;
using System.Data;


namespace MooDb.Core
{
    /// <summary>
    /// Entry point for executing database operations using MooDb.
    /// </summary>
    /// <remarks>
    /// <para>
    /// MooDb provides a lightweight, predictable API for executing stored procedures
    /// and mapping results to .NET types.
    /// </para>
    /// <para>
    /// The API supports:
    /// - non-query execution (<see cref="ExecuteAsync"/>)
    /// - scalar values (<see cref="ScalarAsync{T}"/>)
    /// - result set mapping (<see cref="ListAsync{T}"/> and <see cref="SingleAsync{T}"/>)
    /// </para>
    /// <para>
    /// Auto-mapping supports both:
    /// - classes with public settable properties
    /// - records or types with matching constructor parameters
    /// </para>
    /// <para>
    /// MooDb is designed around a stored procedure-first approach. For SQL text execution,
    /// use the <see cref="Sql"/> property.
    /// </para>
    /// <para>
    /// Instances can be created using either:
    /// - a connection string (MooDb manages connection lifetime)
    /// - an existing <see cref="SqlConnection"/> (caller manages connection lifetime)
    /// </para>
    /// <para>
    /// The API is intentionally minimal, favouring explicit behaviour and predictable performance.
    /// </para>
    /// </remarks>
    public sealed class MooDb
    {
        // Fields
        private readonly MooCommandExecutor _executor;
        private readonly string? _connectionString;
        private readonly SqlConnection? _connection;
        private readonly MooMapper _mapper;


        // Properties
        public MooSql Sql { get; }


        // Constructors
        public MooDb(string connectionString, MooDbOptions? options = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _connection = null;

            var opts = options ?? new MooDbOptions();
            _executor = new MooCommandExecutor(opts.CommandTimeoutSeconds);
            _mapper = new MooMapper(opts.StrictAutoMapping);
            Sql = new MooSql(_executor, _mapper, CreateExecutionContext);
        }

        public MooDb(SqlConnection connection, MooDbOptions? options = null)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _connectionString = null;

            var opts = options ?? new MooDbOptions();
            _executor = new MooCommandExecutor(opts.CommandTimeoutSeconds);
            _mapper = new MooMapper(opts.StrictAutoMapping);
            Sql = new MooSql(_executor, _mapper, CreateExecutionContext);
        }


        // Public API

        /// <summary>
        /// Executes a stored procedure that does not return result sets.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is intended for commands such as inserts, updates, and deletes.
        /// </para>
        /// <para>
        /// The return value represents the number of rows affected, as reported by SQL Server.
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Executes a stored procedure and returns a single scalar value.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the result is <c>null</c> or <see cref="DBNull"/>, <c>null</c> is returned.
        /// </para>
        /// <para>
        /// Otherwise, the value is converted to <typeparamref name="T"/> using light type conversion.
        /// </para>
        /// <para>
        /// This method is not affected by strict auto-mapping settings.
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Executes a stored procedure and maps the result set to a list of <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Auto-mapping supports both:
        /// - classes with public settable properties
        /// - records or types with matching constructor parameters
        /// </para>
        /// <para>
        /// Column and property/parameter names are matched case-insensitively.
        /// Compatible type conversions are applied where possible.
        /// </para>
        /// <para>
        /// If strict auto-mapping is enabled, an exception is thrown when the result set
        /// does not match the target type.
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Executes a stored procedure and returns a single result mapped to <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Returns <c>null</c> if no rows are returned.
        /// </para>
        /// <para>
        /// Returns the mapped object if exactly one row is returned.
        /// </para>
        /// <para>
        /// Throws an exception if more than one row is returned.
        /// </para>
        /// <para>
        /// Mapping rules are the same as <see cref="ListAsync{T}"/>.
        /// </para>
        /// </remarks>
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


        // Private Helpers
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