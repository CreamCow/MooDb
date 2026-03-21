using Microsoft.Data.SqlClient;
using MooDb.Mapping;

namespace MooDb.Core;

/// <summary>
/// Represents multiple result sets returned from a single database command.
/// </summary>
/// <remarks>
/// <para>
/// Allows sequential reading of multiple result sets from a single execution.
/// </para>
/// <para>
/// Each call to <see cref="ReadAsync{T}(CancellationToken)"/> consumes the next result set.
/// </para>
/// <para>
/// Result sets must be read in order. Attempting to read beyond the available result sets
/// will result in an exception.
/// </para>
/// </remarks>
public sealed class MooResults : IAsyncDisposable
{
    // Fields
    private readonly SqlDataReader _reader;
    private readonly MooMapper _mapper;
    private bool _consumed;
    private bool _disposed;


    // Constructors
    internal MooResults(SqlDataReader reader, MooMapper mapper)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }


    // Public API
    /// <summary>
    /// Reads the next result set and maps it to a list of <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each call advances to the next result set.
    /// </para>
    /// <para>
    /// Result sets must be read sequentially and cannot be revisited.
    /// </para>
    /// <para>
    /// Throws an exception if no more result sets are available.
    /// </para>
    /// </remarks>
    public async Task<List<T>> ReadAsync<T>(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_consumed)
        {
            var hasNext = await _reader.NextResultAsync(cancellationToken);

            if (!hasNext)
                throw new InvalidOperationException("No more result sets available.");
        }

        _consumed = true;

        return await _mapper.MapListAsync<T>(_reader, cancellationToken);
    }


    /// <summary>
    /// Asynchronously disposes the underlying data reader.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Disposing the results will close the underlying data reader.
    /// </para>
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        await _reader.DisposeAsync();
        _disposed = true;
    }


    // Internal helpers


    // Private helpers
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MooResults));
    }
}