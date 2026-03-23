using Microsoft.Data.SqlClient;
using MooDb.Mapping;

namespace MooDb;

/// <summary>
/// Represents multiple result sets returned from a single database command.
/// </summary>
/// <remarks>
/// <para>
/// Allows sequential reading of multiple result sets from a single execution.
/// </para>
/// <para>
/// Each call to <see cref="ReadAsync{T}(CancellationToken)"/> or <see cref="ReadSingleAsync{T}(CancellationToken)"/>
/// consumes the next result set.
/// </para>
/// <para>
/// Result sets must be read in order. Attempting to read beyond the available result sets
/// will result in an exception.
/// </para>
/// </remarks>
public sealed class MooResults : IAsyncDisposable
{
    private readonly SqlDataReader _reader;
    private readonly MooMapper _mapper;
    private bool _consumed;
    private bool _disposed;

    internal MooResults(SqlDataReader reader, MooMapper mapper)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <summary>
    /// Reads the next result set and maps all rows to a list of <typeparamref name="T"/> using MooDb automatic mapping.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each call consumes the next unread result set.
    /// </para>
    /// <para>
    /// Automatic mapping uses the same conventions as <see cref="MooDb.ListAsync{T}(string, IReadOnlyList{SqlParameter}?, int?, CancellationToken)"/>.
    /// </para>
    /// </remarks>
    public Task<List<T>> ReadAsync<T>(CancellationToken cancellationToken = default)
        => ReadNextResultAsync(r => _mapper.MapListAsync<T>(r, cancellationToken), cancellationToken);

    /// <summary>
    /// Reads the next result set and maps all rows to a list of <typeparamref name="T"/> using a supplied custom mapper.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each call consumes the next unread result set.
    /// </para>
    /// <para>
    /// The supplied <paramref name="map"/> delegate is invoked once per row and bypasses MooDb automatic mapping.
    /// </para>
    /// </remarks>
    public Task<List<T>> ReadAsync<T>(
        Func<SqlDataReader, T> map,
        CancellationToken cancellationToken = default)
        => ReadNextResultAsync(r => _mapper.MapListAsync(r, map, cancellationToken), cancellationToken);

    /// <summary>
    /// Reads the next result set and returns a single value mapped to <typeparamref name="T"/> using MooDb automatic mapping.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns <c>null</c> if no rows are returned.
    /// </para>
    /// <para>
    /// Throws an <see cref="InvalidOperationException"/> if more than one row is returned.
    /// </para>
    /// </remarks>
    public Task<T?> ReadSingleAsync<T>(CancellationToken cancellationToken = default)
        => ReadNextResultAsync(r => _mapper.MapSingleAsync<T>(r, cancellationToken), cancellationToken);

    /// <summary>
    /// Reads the next result set and returns a single value mapped by a supplied custom mapper.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns <c>null</c> if no rows are returned.
    /// </para>
    /// <para>
    /// Throws an <see cref="InvalidOperationException"/> if more than one row is returned.
    /// </para>
    /// <para>
    /// The supplied <paramref name="map"/> delegate is invoked for each row and bypasses MooDb automatic mapping.
    /// </para>
    /// </remarks>
    public Task<T?> ReadSingleAsync<T>(
        Func<SqlDataReader, T> map,
        CancellationToken cancellationToken = default)
        => ReadNextResultAsync(r => _mapper.MapSingleAsync(r, map, cancellationToken), cancellationToken);

    /// <summary>
    /// Advances to the next result set without reading it.
    /// </summary>
    /// <remarks>
    /// Use this method to skip the current result set and position the reader on the next one.
    /// Returns <c>false</c> when no further result sets are available.
    /// </remarks>
    public async Task<bool> NextResultAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _consumed = true;
        return await _reader.NextResultAsync(cancellationToken);
    }

    /// <summary>
    /// Disposes the underlying data reader and releases database resources associated with these results.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        await _reader.DisposeAsync();
        _disposed = true;
    }

    private async Task<TResult> ReadNextResultAsync<TResult>(
        Func<SqlDataReader, Task<TResult>> read,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        if (_consumed)
        {
            var hasNext = await _reader.NextResultAsync(cancellationToken);

            if (!hasNext)
                throw new InvalidOperationException("No more result sets available.");
        }

        _consumed = true;
        return await read(_reader);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MooResults));
    }
}
