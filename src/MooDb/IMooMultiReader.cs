using Microsoft.Data.SqlClient;

namespace MooDb;

/// <summary>
/// Provides sequential access to multiple result sets returned from a single database command.
/// </summary>
/// <remarks>
/// <para>
/// Each method consumes the next unread result set in the order returned by SQL Server.
/// </para>
/// <para>
/// Result sets cannot be revisited. Attempting to read beyond the available result sets throws an
/// <see cref="InvalidOperationException"/>.
/// </para>
/// <para>
/// This interface is used only within <c>QueryMultipleAsync</c> callbacks. Callers do not create or dispose it directly.
/// </para>
/// </remarks>
public interface IMooMultiReader
{
    /// <summary>
    /// Reads the next result set and returns a single mapped value using MooDb automatic mapping.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <returns>
    /// The mapped value when exactly one row is returned; otherwise <c>null</c> when the result set has no rows.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the result set contains more than one row.</exception>
    T? Single<T>();

    /// <summary>
    /// Reads the next result set and returns a single mapped value using a supplied custom row mapper.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="map">The custom row materialiser.</param>
    /// <returns>
    /// The mapped value when exactly one row is returned; otherwise <c>null</c> when the result set has no rows.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="map"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the result set contains more than one row.</exception>
    T? Single<T>(Func<SqlDataReader, T> map);

    /// <summary>
    /// Reads the next result set and maps all rows to a list using MooDb automatic mapping.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <returns>A list containing all mapped rows from the next result set.</returns>
    IReadOnlyList<T> List<T>();

    /// <summary>
    /// Reads the next result set and maps all rows to a list using a supplied custom row mapper.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="map">The custom row materialiser.</param>
    /// <returns>A list containing all mapped rows from the next result set.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="map"/> is <c>null</c>.</exception>
    IReadOnlyList<T> List<T>(Func<SqlDataReader, T> map);

    /// <summary>
    /// Reads the next result set and returns a required scalar value.
    /// </summary>
    /// <typeparam name="T">The target scalar type.</typeparam>
    /// <returns>The converted scalar value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the result set has no rows or the first value is <see cref="DBNull"/>.</exception>
    T Scalar<T>();

    /// <summary>
    /// Reads the next result set and returns an optional scalar value.
    /// </summary>
    /// <typeparam name="T">The target scalar type.</typeparam>
    /// <returns>The converted scalar value, or <c>default</c> when the result set has no rows or the first value is <see cref="DBNull"/>.</returns>
    T? ScalarOrDefault<T>();
}
