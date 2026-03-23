using Microsoft.Data.SqlClient;
using MooDb.Execution;
using MooDb.Mapping;
using MooDb.Sql;
using System.Data;


namespace MooDb
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
        /// <summary>
        /// Provides access to SQL text execution outside a transaction.
        /// </summary>
        /// <remarks>
        /// MooDb is stored procedure-first. Use this property when you need to execute raw SQL text.
        /// </remarks>
        public MooSql Sql { get; }


        // Constructors
        /// <summary>
        /// Creates a new <see cref="MooDb"/> instance that manages connections using the supplied connection string.
        /// </summary>
        /// <param name="connectionString">The SQL Server connection string.</param>
        /// <param name="options">Optional MooDb configuration settings.</param>
        public MooDb(string connectionString, MooDbOptions? options = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _connection = null;

            var opts = options ?? new MooDbOptions();
            _executor = new MooCommandExecutor(opts.CommandTimeoutSeconds);
            _mapper = new MooMapper(opts.StrictAutoMapping);
            Sql = new MooSql(_executor, _mapper, CreateExecutionContext);
        }

        /// <summary>
        /// Creates a new <see cref="MooDb"/> instance that uses an existing openable <see cref="SqlConnection"/>.
        /// </summary>
        /// <param name="connection">The caller-managed SQL Server connection.</param>
        /// <param name="options">Optional MooDb configuration settings.</param>
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
        /// <para>
        /// The number of affected rows depends on the behaviour of the executed command and may be zero.
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
        /// An exception is thrown if the value cannot be converted to the target type.
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
        /// Throws an <see cref="InvalidOperationException"/> if more than one row is returned.
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
                    return await _mapper.MapSingleAsync<T>(reader, cancellationToken);
                },
                cancellationToken);
        }

        /// <summary>
        /// Executes a stored procedure and returns a single result mapped by a supplied custom mapper.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Returns <c>null</c> if no rows are returned.
        /// </para>
        /// <para>
        /// Returns the mapped object if exactly one row is returned.
        /// </para>
        /// <para>
        /// Throws an <see cref="InvalidOperationException"/> if more than one row is returned.
        /// </para>
        /// <para>
        /// The supplied <paramref name="map"/> delegate is invoked for each row and bypasses MooDb automatic mapping.
        /// </para>
        /// <para>
        /// This is useful when you want explicit materialisation logic, reusable map classes, or lower mapping overhead on hot paths.
        /// </para>
        /// </remarks>
        public Task<T?> SingleAsync<T>(
            string procedure,
            Func<SqlDataReader, T> map,
            IReadOnlyList<SqlParameter>? parameters = null,
            int? commandTimeoutSeconds = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(map);

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
                    return await _mapper.MapSingleAsync(reader, map, cancellationToken);
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
        /// </para>
        /// <para>
        /// Compatible type conversions are applied where possible. An exception is thrown if a value
        /// cannot be assigned to the target property or constructor parameter type.
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
        /// Executes a stored procedure and maps the result set to a list of <typeparamref name="T"/> using a supplied custom mapper.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The supplied <paramref name="map"/> delegate is invoked once per row and bypasses MooDb automatic mapping.
        /// </para>
        /// <para>
        /// This is useful when the result shape does not map cleanly by convention, when you want reusable map classes, or when you want precise control over materialisation.
        /// </para>
        /// <para>
        /// A custom mapper can also reduce mapping overhead for large result sets or hot paths.
        /// </para>
        /// </remarks>
        public Task<List<T>> ListAsync<T>(
            string procedure,
            Func<SqlDataReader, T> map,
            IReadOnlyList<SqlParameter>? parameters = null,
            int? commandTimeoutSeconds = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(map);

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
                    return await _mapper.MapListAsync(reader, map, cancellationToken);
                },
                cancellationToken);
        }

        /// <summary>
        /// Executes a stored procedure and returns multiple result sets.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Executes the stored procedure and returns a <see cref="MooResults"/> instance
        /// for sequentially reading result sets.
        /// </para>
        /// <para>
        /// Each call to <see cref="MooResults.ReadAsync{T}(CancellationToken)"/> consumes the next result set.
        /// </para>
        /// <para>
        /// Result sets must be read in order and cannot be revisited.
        /// </para>
        /// <para>
        /// The underlying data reader is owned by <see cref="MooResults"/> and is disposed
        /// when the results are disposed.
        /// </para>
        /// </remarks>
        public Task<MooResults> QueryMultipleAsync(
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
                    var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                    return new MooResults(reader, _mapper);
                },
                cancellationToken);
        }

        /// <summary>
        /// Begins a new database transaction.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Creates a connection (if required) and starts a SQL Server transaction using the specified isolation level.
        /// </para>
        /// <para>
        /// All commands executed through the returned <see cref="MooTransaction"/> share the same
        /// connection and transaction.
        /// </para>
        /// <para>
        /// Changes are committed only when <see cref="MooTransaction.CommitAsync(CancellationToken)"/> is called.
        /// If the transaction is disposed without being committed, it is rolled back.
        /// </para>
        /// <para>
        /// If MooDb was created with a connection string, the connection is owned by the transaction
        /// and disposed when the transaction is disposed.
        /// </para>
        /// <para>
        /// If MooDb was created with an existing <see cref="SqlConnection"/>, the connection is reused
        /// and is not disposed by MooDb.
        /// </para>
        /// </remarks>
        public async Task<MooTransaction> BeginTransactionAsync(
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default)
        {
            var ownsConnection = _connection is null;

            var connection = _connection ?? new SqlConnection(_connectionString!);

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            var transaction = (SqlTransaction)await connection.BeginTransactionAsync(isolationLevel, cancellationToken);

            return new MooTransaction(
                _executor,
                _mapper,
                connection,
                transaction,
                ownsConnection);
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