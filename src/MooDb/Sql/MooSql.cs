using Microsoft.Data.SqlClient;
using MooDb.Execution;
using MooDb.Mapping;
using System.Data;

namespace MooDb.Sql;

/// <summary>
/// Provides raw SQL execution for MooDb.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MooSql"/> mirrors the core query surface of <see cref="MooDb.MooDb"/>, but executes SQL text instead of stored procedures.
/// </para>
/// <para>
/// MooDb is designed around a stored procedure-first model. This type exists as an explicit SQL text escape hatch.
/// </para>
/// </remarks>
public sealed class MooSql
{
    // Fields
    private readonly MooCommandExecutor _executor;
    private readonly MooMapper _mapper;
    private readonly Func<MooExecutionContext> _contextFactory;

    // Constructors
    internal MooSql(
        MooCommandExecutor executor,
        MooMapper mapper,
        Func<MooExecutionContext> contextFactory)
    {
        _executor = executor;
        _mapper = mapper;
        _contextFactory = contextFactory;
    }


    // Public API

    /// <summary>
    /// Executes a SQL text command that does not return result sets.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method executes the provided SQL text directly against the database.
    /// </para>
    /// <para>
    /// It is intended for commands such as inserts, updates, and deletes.
    /// </para>
    /// <para>
    /// The return value represents the number of rows affected, as reported by SQL Server.
    /// </para>
    /// <para>
    /// MooDb is designed for stored procedure usage. This method provides a SQL text
    /// alternative where needed.
    /// </para>
    /// </remarks>
    public Task<int> ExecuteAsync(
        string sql,
        IReadOnlyList<SqlParameter>? parameters = null,
        int? commandTimeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        var context = _contextFactory();

        return _executor.ExecuteAsync(
            context,
            sql,
            CommandType.Text,
            parameters,
            commandTimeoutSeconds,
            cmd => cmd.ExecuteNonQueryAsync(cancellationToken),
            cancellationToken);
    }

        /// <summary>
    /// Executes a SQL text query and returns a scalar value.
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
    /// MooDb is designed for stored procedure usage. This method provides a SQL text alternative where needed.
    /// </para>
    /// </remarks>
    public Task<T> ScalarAsync<T>(
        string sql,
        IReadOnlyList<SqlParameter>? parameters = null,
        int? commandTimeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        var context = _contextFactory();

        return _executor.ExecuteAsync(
            context,
            sql,
            CommandType.Text,
            parameters,
            commandTimeoutSeconds,
            async cmd => MooScalarConverter.ConvertOrDefault<T>(await cmd.ExecuteScalarAsync(cancellationToken)),
            cancellationToken);
    }

    /// <summary>
    /// Executes a SQL text query and returns a single result mapped to <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method executes the provided SQL text directly against the database.
    /// </para>
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
    /// <para>
    /// MooDb is designed for stored procedure usage. This method provides a SQL text
    /// alternative where needed.
    /// </para>
    /// </remarks>
    public Task<T?> SingleAsync<T>(
        string sql,
        IReadOnlyList<SqlParameter>? parameters = null,
        int? commandTimeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        var context = _contextFactory();

        return _executor.ExecuteAsync(
            context,
            sql,
            CommandType.Text,
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
    /// Executes a SQL text query and returns a single result mapped by a supplied custom mapper.
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
    /// MooDb is designed for stored procedure usage. This overload provides a SQL text alternative with explicit row materialisation.
    /// </para>
    /// </remarks>
    public Task<T?> SingleAsync<T>(
        string sql,
        Func<SqlDataReader, T> map,
        IReadOnlyList<SqlParameter>? parameters = null,
        int? commandTimeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(map);

        var context = _contextFactory();

        return _executor.ExecuteAsync(
            context,
            sql,
            CommandType.Text,
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
    /// Executes a SQL text query and maps the result set to a list of <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method executes the provided SQL text directly against the database.
    /// </para>
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
    /// <para>
    /// MooDb is designed for stored procedure usage. This method provides a SQL text
    /// alternative where needed.
    /// </para>
    /// </remarks>
    public Task<List<T>> ListAsync<T>(
        string sql,
        IReadOnlyList<SqlParameter>? parameters = null,
        int? commandTimeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        var context = _contextFactory();

        return _executor.ExecuteAsync(
            context,
            sql,
            CommandType.Text,
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
    /// Executes a SQL text query and maps the result set to a list of <typeparamref name="T"/> using a supplied custom mapper.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The supplied <paramref name="map"/> delegate is invoked once per row and bypasses MooDb automatic mapping.
    /// </para>
    /// <para>
    /// This is useful when the result shape does not map cleanly by convention, when you want reusable map classes, or when you want precise control over materialisation.
    /// </para>
    /// <para>
    /// MooDb is designed for stored procedure usage. This overload provides a SQL text alternative with explicit row materialisation.
    /// </para>
    /// </remarks>
    public Task<List<T>> ListAsync<T>(
        string sql,
        Func<SqlDataReader, T> map,
        IReadOnlyList<SqlParameter>? parameters = null,
        int? commandTimeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(map);

        var context = _contextFactory();

        return _executor.ExecuteAsync(
            context,
            sql,
            CommandType.Text,
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
    /// Executes a SQL text query and materialises multiple result sets into a caller-defined result object.
    /// </summary>
    /// <typeparam name="TResult">The final result type returned by the callback.</typeparam>
    /// <param name="sql">The SQL text to execute.</param>
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
        string sql,
        Func<IMooMultiReader, TResult> read,
        IReadOnlyList<SqlParameter>? parameters = null,
        int? commandTimeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(read);

        var context = _contextFactory();

        return _executor.ExecuteAsync(
            context,
            sql,
            CommandType.Text,
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

}