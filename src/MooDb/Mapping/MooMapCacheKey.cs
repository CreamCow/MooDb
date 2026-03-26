namespace MooDb.Mapping;


/// <summary>
/// Represents a unique key for identifying a mapping plan based on target type,
/// strict auto-mapping configuration, and result set shape.
/// </summary>
/// <remarks>
/// The cache key is composed of:
/// - the target CLR type being mapped to
/// - whether strict auto-mapping is enabled
/// - the ordered list of column names returned by the query
///
/// Column order is significant because mapping relies on column ordinals for performance.
/// Two result sets with the same columns in a different order are treated as distinct shapes.
///
/// Column name comparison is case-insensitive to align with typical database behaviour.
/// </remarks>
internal sealed class MooMapCacheKey : IEquatable<MooMapCacheKey>
{
    internal Type TargetType { get; }
    internal bool StrictAutoMapping { get; }
    internal string[] Columns { get; }

    internal MooMapCacheKey(Type targetType, bool strictAutoMapping, string[] columns)
    {
        TargetType = targetType;
        StrictAutoMapping = strictAutoMapping;
        Columns = columns;
    }

    public bool Equals(MooMapCacheKey? other)
    {
        if (other is null) return false;
        if (TargetType != other.TargetType) return false;
        if (StrictAutoMapping != other.StrictAutoMapping) return false;
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
        hash.Add(StrictAutoMapping);

        foreach (var column in Columns)
        {
            hash.Add(column, StringComparer.OrdinalIgnoreCase);
        }

        return hash.ToHashCode();
    }
}