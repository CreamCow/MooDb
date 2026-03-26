using System.Data;
using Microsoft.Data.SqlClient;
using MooDb.Bulk;
using MooDb.Execution;
using MooDb.Mapping;
using MooDb.Sql;

namespace MooDb;

/// <summary>
/// Represents an active database transaction for executing multiple operations as a single unit of work.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="MooTransaction"/> provides the same core API surface as <see cref="MooDb"/>,
/// but all commands execute on the same connection and within the same SQL Server transaction.
/// </para>
/// <para>
/// Use <see cref="global::MooDb.MooDb.BeginTransactionAsync"/> to create an instance.
/// </para>
/// <para>
/// Changes are committed only when <see cref="CommitAsync(CancellationToken)"/> is called.
/// If the transaction is disposed without being committed, MooDb attempts to roll it back automatically.
/// </para>
/// <para>
/// After <see cref="CommitAsync(CancellationToken)"/>, the transaction should be considered complete and disposed.
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
        Bulk = new MooBulk(CreateExecutionContext);
    }


    // Public API
    /// <summary>
    /// Provides access to SQL text execution within the current transaction.
    /// </summary>
    public MooSql Sql { get; }

    /// <summary>
    /// Provides access to bulk insert operations within the current transaction.
    /// </summary>
    /// <remarks>
    /// Use this property when you want to copy many rows directly into a SQL Server table
    /// as part of the current transaction.
    /// </remarks>
    public MooBulk Bulk { get; }
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
        ThrowIfCompleted();
        MooGuard.AgainstNullOrWhiteSpace(procedure, nameof(procedure), "Procedure name");

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
    /// Executes a stored procedure and returns a scalar value within the current transaction.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The first column of the first row is converted to <typeparamref name="T"/>.
    /// </para>
    /// <para>
    /// Returns <c>default</c> when no rows are returned or the first value is <see cref="DBNull"/>.
    /// Use a nullable type such as <c>int?</c> when absence must be distinguishable from a non-null default value.
    /// </para>
    /// <para>
    /// This method is not affected by strict auto-mapping settings.
    /// </para>
    /// </remarks>
    public Task<T> ScalarAsync<T>(
        string procedure,
        IReadOnlyList<SqlParameter>? parameters = null,
        int? commandTimeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfCompleted();
        MooGuard.AgainstNullOrWhiteSpace(procedure, nameof(procedure), "Procedure name");

        var context = CreateExecutionContext();

        return _executor.ExecuteAsync(
            context,
            procedure,
            CommandType.StoredProcedure,
            parameters,
            commandTimeoutSeconds,
            async cmd => MooScalarConverter.ConvertScalarOrDefault<T>(await cmd.ExecuteScalarAsync(cancellationToken)),
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
    /// Throws an <see cref="InvalidOperationException"/> if more than one row is returned.
    /// </para>
    /// <para>
    /// Mapping rules are the same as <see cref="ListAsync{T}(string, System.Collections.Generic.IReadOnlyList{Microsoft.Data.SqlClient.SqlParameter}?, int?, System.Threading.CancellationToken)"/>.
    /// </para>
    /// </remarks>
    public Task<T?> SingleAsync<T>(
        string procedure,
        IReadOnlyList<SqlParameter>? parameters = null,
        int? commandTimeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfCompleted();
        MooGuard.AgainstNullOrWhiteSpace(procedure, nameof(procedure), "Procedure name");

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
        ThrowIfCompleted();
        ArgumentNullException.ThrowIfNull(map);
        MooGuard.AgainstNullOrWhiteSpace(procedure, nameof(procedure), "Procedure name");

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
        ThrowIfCompleted();
        MooGuard.AgainstNullOrWhiteSpace(procedure, nameof(procedure), "Procedure name");

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
        ThrowIfCompleted();
        ArgumentNullException.ThrowIfNull(map);
        MooGuard.AgainstNullOrWhiteSpace(procedure, nameof(procedure), "Procedure name");

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
    /// Executes a stored procedure within the current transaction and materialises multiple result sets into a caller-defined result object.
    /// </summary>
    /// <typeparam name="TResult">The final result type returned by the callback.</typeparam>
    /// <param name="procedure">The stored procedure name.</param>
    /// <param name="read">
    /// A callback that consumes result sets sequentially through <see cref="IMooMultiReader"/> and returns the final materialised result.
    /// </param>
    /// <param name="parameters">Optional command parameters.</param>
    /// <param name="commandTimeoutSeconds">An optional per-command timeout override, in seconds.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The caller-defined result produced by <paramref name="read"/>.</returns>
    /// <remarks>
    /// <para>
    /// Each call on <paramref name="read"/> consumes the next result set in order.
    /// </para>
    /// <para>
    /// MooDb fully materialises the results inside the method call, so callers do not manage live reader objects.
    /// </para>
    /// <para>
    /// Attempting to read beyond the available result sets throws an <see cref="InvalidOperationException"/>.
    /// </para>
    /// </remarks>
    public Task<TResult> QueryMultipleAsync<TResult>(
        string procedure,
        Func<IMooMultiReader, TResult> read,
        IReadOnlyList<SqlParameter>? parameters = null,
        int? commandTimeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfCompleted();
        ArgumentNullException.ThrowIfNull(read);
        MooGuard.AgainstNullOrWhiteSpace(procedure, nameof(procedure), "Procedure name");

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
                var multiReader = new MooMultiReader(reader, _mapper);
                return read(multiReader);
            },
            cancellationToken);
    }

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Once committed, the transaction work is complete. Dispose the transaction when you are finished with it.
    /// </para>
    /// <para>
    /// Dispose the transaction after commit to release the underlying transaction and, when owned, its connection.
    /// </para>
    /// </remarks>
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfCompleted();

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
    /// The underlying SQL transaction is always disposed. The connection is disposed only when this <see cref="MooTransaction"/> owns it.
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


    // Private helpers
    private MooExecutionContext CreateExecutionContext()
    {
        ThrowIfCompleted();
        return new MooExecutionContext(_connection, _transaction, ownsConnection: false);
    }


    private void ThrowIfCompleted()
    {
        ThrowIfDisposed();

        if (_committed)
        {
            throw new InvalidOperationException(
                "This transaction has already been committed and can no longer be used.");
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}