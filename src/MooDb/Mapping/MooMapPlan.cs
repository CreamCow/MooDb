using Microsoft.Data.SqlClient;

namespace MooDb.Mapping;


/// <summary>
/// Represents a compiled mapping plan for converting a data reader row into an instance of <typeparamref name="T"/>.
/// </summary>
/// <remarks>
/// A mapping plan is created once per unique combination of target type and result set shape,
/// and then reused for subsequent rows and executions.
///
/// The plan supports both constructor-based and property-based mapping:
/// - For record or constructor-based types, values are passed into the constructor via the <see cref="Create"/> delegate.
/// - For types with writable properties, values may be assigned after creation via the optional <see cref="Assign"/> delegate.
///
/// This allows MooDb to support both immutable records and mutable DTO-style classes.
///
/// By compiling and caching these delegates, MooDb avoids repeated reflection during row mapping.
/// </remarks>
internal sealed class MooMapPlan<T>
{
    public Func<SqlDataReader, T> Create { get; }
    public Action<T, SqlDataReader>? Assign { get; }

    public MooMapPlan(
        Func<SqlDataReader, T> create,
        Action<T, SqlDataReader>? assign)
    {
        Create = create;
        Assign = assign;
    }
}