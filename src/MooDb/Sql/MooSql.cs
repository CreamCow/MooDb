using Microsoft.Data.SqlClient;
using MooDb.Core;
using MooDb.Execution;
using MooDb.Mapping;
using System.Data;

namespace MooDb.Sql;

/// <summary>
/// Provides raw SQL execution for MooDb.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MooSql"/> mirrors the core query surface of <see cref="MooDb.Core.MooDb"/>, but executes SQL text instead of stored procedures.
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
    /// Executes a SQL text query and returns a single scalar value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method executes the provided SQL text directly against the database.
    /// </para>
    /// <para>
    /// If the result is <c>null</c> or <see cref="DBNull"/>, <c>null</c> is returned.
    /// </para>
    /// <para>
    /// Otherwise, the value is converted to <typeparamref name="T"/> using light type conversion.
    /// </para>
    /// <para>
    /// This method is not affected by strict auto-mapping settings.
    /// </para>
    /// <para>
    /// MooDb is designed for stored procedure usage. This method provides a SQL text
    /// alternative where needed.
    /// </para>
    /// </remarks>
    public Task<T?> ScalarAsync<T>(
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
                var result = await cmd.ExecuteScalarAsync(cancellationToken);

                if (result is null || result is DBNull)
                    return default;

                return (T)Convert.ChangeType(result, typeof(T));
            },
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
    /// Executes a SQL text query and returns multiple result sets.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Executes the provided SQL text directly against the database and returns a <see cref="MooResults"/>
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
    /// <para>
    /// MooDb is designed for stored procedure usage. This method provides a SQL text
    /// alternative where needed.
    /// </para>
    /// </remarks>
    public Task<MooResults> QueryMultipleAsync(
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
                var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                return new MooResults(reader, _mapper);
            },
            cancellationToken);
    }
}