using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Data.SqlClient;

namespace MooDb.Mapping;

internal sealed class MooMapper
{
    private readonly bool _strictAutoMapping;

    internal MooMapper(bool strictAutoMapping)
    {
        _strictAutoMapping = strictAutoMapping;
    }

    internal List<T> MapList<T>(SqlDataReader reader)
    {
        var plan = GetOrCreatePlan<T>(reader);
        return MapList(reader, plan.Create, plan.Assign);
    }

    internal List<T> MapList<T>(
        SqlDataReader reader,
        Func<SqlDataReader, T> map)
    {
        ArgumentNullException.ThrowIfNull(map);
        return MapList(reader, map, assign: null);
    }

    internal T? MapSingle<T>(SqlDataReader reader)
    {
        var plan = GetOrCreatePlan<T>(reader);
        return MapSingle(reader, plan.Create, plan.Assign);
    }

    internal T? MapSingle<T>(
        SqlDataReader reader,
        Func<SqlDataReader, T> map)
    {
        ArgumentNullException.ThrowIfNull(map);
        return MapSingle(reader, map, assign: null);
    }

    internal async Task<List<T>> MapListAsync<T>(
        SqlDataReader reader,
        CancellationToken cancellationToken = default)
    {
        var plan = GetOrCreatePlan<T>(reader);
        return await MapListAsync(reader, plan.Create, plan.Assign, cancellationToken);
    }

    internal Task<List<T>> MapListAsync<T>(
        SqlDataReader reader,
        Func<SqlDataReader, T> map,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(map);
        return MapListAsync(reader, map, assign: null, cancellationToken);
    }

    internal async Task<T?> MapSingleAsync<T>(
        SqlDataReader reader,
        CancellationToken cancellationToken = default)
    {
        var plan = GetOrCreatePlan<T>(reader);
        return await MapSingleAsync(reader, plan.Create, plan.Assign, cancellationToken);
    }

    internal Task<T?> MapSingleAsync<T>(
        SqlDataReader reader,
        Func<SqlDataReader, T> map,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(map);
        return MapSingleAsync(reader, map, assign: null, cancellationToken);
    }

    private static List<T> MapList<T>(
        SqlDataReader reader,
        Func<SqlDataReader, T> create,
        Action<T, SqlDataReader>? assign)
    {
        var results = new List<T>();

        while (reader.Read())
        {
            var instance = create(reader);
            assign?.Invoke(instance, reader);
            results.Add(instance);
        }

        return results;
    }

    private static async Task<List<T>> MapListAsync<T>(
        SqlDataReader reader,
        Func<SqlDataReader, T> create,
        Action<T, SqlDataReader>? assign,
        CancellationToken cancellationToken)
    {
        var results = new List<T>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var instance = create(reader);
            assign?.Invoke(instance, reader);
            results.Add(instance);
        }

        return results;
    }

    private static T? MapSingle<T>(
        SqlDataReader reader,
        Func<SqlDataReader, T> create,
        Action<T, SqlDataReader>? assign)
    {
        if (!reader.Read())
            return default;

        var instance = create(reader);
        assign?.Invoke(instance, reader);

        if (reader.Read())
        {
            throw new InvalidOperationException("Expected at most one row but received more than one.");
        }

        return instance;
    }

    private static async Task<T?> MapSingleAsync<T>(
        SqlDataReader reader,
        Func<SqlDataReader, T> create,
        Action<T, SqlDataReader>? assign,
        CancellationToken cancellationToken)
    {
        if (!await reader.ReadAsync(cancellationToken))
            return default;

        var instance = create(reader);
        assign?.Invoke(instance, reader);

        if (await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Expected at most one row but received more than one.");
        }

        return instance;
    }

    private MooMapPlan<T> GetOrCreatePlan<T>(SqlDataReader reader)
    {
        var key = CreateCacheKey<T>(reader);

        return MooMappingCache.GetOrAdd(key, () => BuildPlan<T>(reader));
    }

    private MooMapCacheKey CreateCacheKey<T>(SqlDataReader reader)
    {
        var fieldCount = reader.FieldCount;
        var columns = new string[fieldCount];

        for (int i = 0; i < fieldCount; i++)
        {
            columns[i] = reader.GetName(i);
        }

        return new MooMapCacheKey(typeof(T), columns);
    }

    private MooMapPlan<T> BuildPlan<T>(SqlDataReader reader)
    {
        var type = typeof(T);

        var ctor = FindUsableConstructor<T>(reader);

        if (ctor is not null)
        {
            var create = CreateConstructorDelegate<T>(ctor, reader);

            return new MooMapPlan<T>(create, assign: null);
        }

        var properties = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToArray();

        if (properties.Length == 0)
        {
            throw new InvalidOperationException(
                $"Type '{type.Name}' must have either a matching constructor or writable properties for auto-mapping.");
        }

        var columnLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < reader.FieldCount; i++)
        {
            columnLookup[reader.GetName(i)] = i;
        }

        var mappings = new List<(int Ordinal, Action<T, object?> Setter)>();

        foreach (var property in properties)
        {
            if (columnLookup.TryGetValue(property.Name, out var ordinal))
            {
                var setter = CreateSetter<T>(property);
                mappings.Add((ordinal, setter));
            }
            else if (_strictAutoMapping)
            {
                throw new InvalidOperationException(
                    $"No matching column found for property '{property.Name}' on type '{type.Name}'.");
            }
        }

        if (_strictAutoMapping)
        {
            foreach (var column in columnLookup.Keys)
            {
                if (!properties.Any(p => string.Equals(p.Name, column, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException(
                        $"No matching property found for column '{column}' on type '{type.Name}'.");
                }
            }
        }

        var createInstance = CreateDefaultConstructor<T>();

        void Assign(T instance, SqlDataReader r)
        {
            foreach (var (ordinal, setter) in mappings)
            {
                var value = r.IsDBNull(ordinal) ? null : r.GetValue(ordinal);
                setter(instance, value);
            }
        }

        return new MooMapPlan<T>(createInstance, Assign);
    }

    private ConstructorInfo? FindUsableConstructor<T>(SqlDataReader reader)
    {
        var type = typeof(T);

        var constructors = type.GetConstructors();

        var columnNames = new HashSet<string>(
            Enumerable.Range(0, reader.FieldCount)
                      .Select(reader.GetName),
            StringComparer.OrdinalIgnoreCase);

        foreach (var ctor in constructors.OrderByDescending(c => c.GetParameters().Length))
        {
            var parameters = ctor.GetParameters();

            if (parameters.Length == 0)
                continue;

            if (parameters.All(p => columnNames.Contains(p.Name!)))
            {
                return ctor;
            }
        }

        return null;
    }

    private Func<SqlDataReader, T> CreateConstructorDelegate<T>(
        ConstructorInfo ctor,
        SqlDataReader reader)
    {
        var parameters = ctor.GetParameters();

        var columnLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < reader.FieldCount; i++)
        {
            columnLookup[reader.GetName(i)] = i;
        }

        var ordinals = parameters
            .Select(p => columnLookup[p.Name!])
            .ToArray();

        return r =>
        {
            var args = new object?[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var value = r.IsDBNull(ordinals[i]) ? null : r.GetValue(ordinals[i]);

                args[i] = ConvertValue(value, parameters[i].ParameterType);
            }

            return (T)ctor.Invoke(args);
        };
    }

    private static Func<SqlDataReader, T> CreateDefaultConstructor<T>()
    {
        var newExpr = Expression.New(typeof(T));
        var lambda = Expression.Lambda<Func<T>>(newExpr);
        var ctor = lambda.Compile();

        return _ => ctor();
    }

    private static Action<T, object?> CreateSetter<T>(PropertyInfo property)
    {
        var instance = Expression.Parameter(typeof(T), "instance");
        var value = Expression.Parameter(typeof(object), "value");

        var convertedValue = Expression.Convert(
            Expression.Call(
                typeof(MooMapper),
                nameof(ConvertValue),
                null,
                value,
                Expression.Constant(property.PropertyType)
            ),
            property.PropertyType);

        var propertyAccess = Expression.Property(instance, property);

        var assign = Expression.Assign(propertyAccess, convertedValue);

        var lambda = Expression.Lambda<Action<T, object?>>(assign, instance, value);

        return lambda.Compile();
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value is null)
            return null;

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlying.IsInstanceOfType(value))
            return value;

        return Convert.ChangeType(value, underlying);
    }
}
