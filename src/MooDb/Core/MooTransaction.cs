using System.Data;
using Microsoft.Data.SqlClient;
using MooDb.Execution;
using MooDb.Mapping;
using MooDb.Sql;

namespace MooDb.Core;

/// <summary>
/// Represents an active database transaction for executing multiple operations as a single unit of work.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="MooTransaction"/> provides the same core API surface as <see cref="MooDb"/>,
/// but all commands execute on the same connection and within the same SQL Server transaction.
/// </para>
/// <para>
/// Use <see cref="MooDb.BeginTransactionAsync(CancellationToken)"/> to create an instance.
/// </para>
/// <para>
/// Changes are committed only when <see cref="CommitAsync(CancellationToken)"/> is called.
/// If the transaction is disposed without being committed, it is rolled back.
/// </para>
/// <para>
/// For SQL text execution, use the <see cref="Sql"/> property.
/// </para>
/// </remarks>
public sealed class MooTransaction : IAsyncDisposable
{
    // Fields
    private readonly MooCommandExecutor _executor;
    private readonly MooMapper _mapper;
    private readonly SqlConnection _connection;
    private readonly SqlTransaction _transaction;
    private readonly bool _ownsConnection;
    private bool _committed;
    private bool _disposed;


    // Constructors
    internal MooTransaction(
        MooCommandExecutor executor,
        MooMapper mapper,
        SqlConnection connection,
        SqlTransaction transaction,
        bool ownsConnection)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        _ownsConnection = ownsConnection;

        Sql = new MooSql(_executor, _mapper, CreateExecutionContext);
    }


    // Public API
    /// <summary>
    /// Provides access to SQL text execution within the current transaction.
    /// </summary>
    public MooSql Sql { get; }

    /// <summary>
    /// Executes a stored procedure that does not return result sets within the current transaction.
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
    /// Executes a stored procedure and returns a single scalar value within the current transaction.
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
    /// Executes a stored procedure and returns a single result mapped to <typeparamref name="T"/> within the current transaction.
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
                return await _mapper.MapSingleAsync<T>(reader, cancellationToken);
            },
            cancellationToken);
    }

    /// <summary>
    /// Executes a stored procedure and returns a single result mapped by a supplied custom mapper within the current transaction.
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
    /// Executes a stored procedure and maps the result set to a list of <typeparamref name="T"/> within the current transaction.
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
    /// Executes a stored procedure and maps the result set to a list of <typeparamref name="T"/> using a supplied custom mapper within the current transaction.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The supplied <paramref name="map"/> delegate is invoked once per row and bypasses MooDb automatic mapping.
    /// </para>
    /// <para>
    /// This is useful when the result shape does not map cleanly by convention, when you want reusable map classes, or when you want precise control over materialisation.
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
    /// Executes a stored procedure within the current transaction and returns multiple result sets.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Executes the stored procedure within the current transaction and returns a <see cref="MooResults"/>
    /// instance for sequentially reading result sets.
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
    /// Commits the current transaction.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Once committed, the transaction cannot be used again.
    /// </para>
    /// <para>
    /// If a transaction is disposed without being committed, MooDb rolls it back.
    /// </para>
    /// </remarks>
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await _transaction.CommitAsync(cancellationToken);
        _committed = true;
    }

    /// <summary>
    /// Asynchronously disposes the transaction.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If the transaction has not been committed, MooDb attempts to roll it back before disposing resources.
    /// </para>
    /// <para>
    /// The underlying connection is always disposed when the transaction is disposed.
    /// </para>
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        try
        {
            if (!_committed)
            {
                await _transaction.RollbackAsync();
            }
        }
        finally
        {
            await _transaction.DisposeAsync();

            if (_ownsConnection)
            {
                await _connection.DisposeAsync();
            }

            _disposed = true;
        }
    }


    // Internal helpers


    // Private helpers
    private MooExecutionContext CreateExecutionContext()
        => new(_connection, _transaction, ownsConnection: false);

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MooTransaction));
    }
}
