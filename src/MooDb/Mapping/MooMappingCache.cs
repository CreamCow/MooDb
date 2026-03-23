using System.Collections.Concurrent;

namespace MooDb.Mapping;

/// <summary>
/// Provides a process-wide cache of compiled mapping plans.
/// </summary>
/// <remarks>
/// Mapping plans are expensive to build due to reflection and delegate compilation.
/// This cache ensures that each unique mapping plan is created only once and reused across all MooDb instances.
///
/// The cache is:
/// - static and shared across the application
/// - thread-safe via <see cref="ConcurrentDictionary{TKey, TValue}"/>
///
/// Only mapping metadata is cached. No data from result sets is stored.
/// </remarks>
internal static class MooMappingCache
{
    private static readonly ConcurrentDictionary<MooMapCacheKey, object> _cache = new();

    internal static MooMapPlan<T> GetOrAdd<T>(
        MooMapCacheKey key,
        Func<MooMapPlan<T>> factory)
    {
        var plan = _cache.GetOrAdd(key, _ => factory());

        return (MooMapPlan<T>)plan;
    }
}