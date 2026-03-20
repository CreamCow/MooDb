namespace MooDb.Mapping;


/// <summary>
/// Represents a unique key for identifying a mapping plan based on target type and result set shape.
/// </summary>
/// <remarks>
/// The cache key is composed of:
/// - the target CLR type being mapped to
/// - the ordered list of column names returned by the query
///
/// Column order is significant because mapping relies on column ordinals for performance.
/// Two result sets with the same columns in a different order are treated as distinct shapes.
///
/// Column name comparison is case-insensitive to align with typical database behaviour.
/// </remarks>
internal sealed class MooMapCacheKey : IEquatable<MooMapCacheKey>
{
    public Type TargetType { get; }
    public string[] Columns { get; }

    public MooMapCacheKey(Type targetType, string[] columns)
    {
        TargetType = targetType;
        Columns = columns;
    }

    public bool Equals(MooMapCacheKey? other)
    {
        if (other is null) return false;
        if (TargetType != other.TargetType) return false;
        if (Columns.Length != other.Columns.Length) return false;

        for (int i = 0; i < Columns.Length; i++)
        {
            if (!string.Equals(Columns[i], other.Columns[i], StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj)
        => Equals(obj as MooMapCacheKey);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(TargetType);

        foreach (var column in Columns)
        {
            hash.Add(column, StringComparer.OrdinalIgnoreCase);
        }

        return hash.ToHashCode();
    }
}