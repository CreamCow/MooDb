using Microsoft.Data.SqlClient;
using MooDb.Mapping;

namespace MooDb;

internal sealed class MooMultiReader : IMooMultiReader
{
    private readonly SqlDataReader _reader;
    private readonly MooMapper _mapper;
    private bool _started;

    internal MooMultiReader(SqlDataReader reader, MooMapper mapper)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public T? Single<T>()
    {
        PrepareNextResult();
        return _mapper.MapSingle<T>(_reader);
    }

    public T? Single<T>(Func<SqlDataReader, T> map)
    {
        ArgumentNullException.ThrowIfNull(map);
        PrepareNextResult();
        return _mapper.MapSingle(_reader, map);
    }

    public IReadOnlyList<T> List<T>()
    {
        PrepareNextResult();
        return _mapper.MapList<T>(_reader);
    }

    public IReadOnlyList<T> List<T>(Func<SqlDataReader, T> map)
    {
        ArgumentNullException.ThrowIfNull(map);
        PrepareNextResult();
        return _mapper.MapList(_reader, map);
    }

    public T Scalar<T>()
    {
        PrepareNextResult();
        return MooScalarConverter.ConvertRequired<T>(ReadScalarValue());
    }

    public T? ScalarOrDefault<T>()
    {
        PrepareNextResult();
        return MooScalarConverter.ConvertOrDefault<T>(ReadScalarValue());
    }

    private void PrepareNextResult()
    {
        if (_started)
        {
            if (!_reader.NextResult())
            {
                throw new InvalidOperationException("No more result sets are available.");
            }

            return;
        }

        _started = true;
    }

    private object? ReadScalarValue()
    {
        if (!_reader.Read())
        {
            return null;
        }

        var value = _reader.IsDBNull(0) ? DBNull.Value : _reader.GetValue(0);

        if (_reader.Read())
        {
            throw new InvalidOperationException("Expected at most one row but received more than one.");
        }

        return value;
    }
}
