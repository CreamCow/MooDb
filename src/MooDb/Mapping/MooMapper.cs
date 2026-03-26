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
            throw new InvalidOperationException(MooErrorMessages.ExpectedSingleRow);
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
            throw new InvalidOperationException(MooErrorMessages.ExpectedSingleRow);
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

        return new MooMapCacheKey(typeof(T), _strictAutoMapping, columns);
    }

    private MooMapPlan<T> BuildPlan<T>(SqlDataReader reader)
    {
        var type = typeof(T);
        var columnLookup = CreateColumnLookup(reader);
        var writableProperties = GetWritableProperties(type);
        var ctorCandidate = FindBestConstructorCandidate<T>(reader);

        if (ctorCandidate is not null)
        {
            if (_strictAutoMapping)
            {
                ValidateStrictConstructorMapping(
                    type,
                    columnLookup,
                    writableProperties,
                    ctorCandidate.ParameterNames,
                    reader);
            }

            var create = CreateConstructorDelegate<T>(ctorCandidate.Constructor, reader);
            var propertyMappings = CreatePropertyMappings<T>(
                writableProperties,
                columnLookup,
                skipPropertyNames: ctorCandidate.ParameterNames,
                validatePotentialConversions: _strictAutoMapping,
                reader);

            if (propertyMappings.Count == 0)
            {
                return new MooMapPlan<T>(create, assign: null);
            }

            void Assign(T instance, SqlDataReader r)
            {
                foreach (var (ordinal, setter) in propertyMappings)
                {
                    var value = r.IsDBNull(ordinal) ? null : r.GetValue(ordinal);
                    setter(instance, value);
                }
            }

            return new MooMapPlan<T>(create, Assign);
        }

        if (writableProperties.Length == 0)
        {
            throw new InvalidOperationException(
                $"Type '{type.Name}' must have either a matching constructor or writable properties for auto-mapping.");
        }

        if (_strictAutoMapping)
        {
            ValidateStrictPropertyMapping(type, columnLookup, writableProperties, reader);
        }

        var mappings = CreatePropertyMappings<T>(
            writableProperties,
            columnLookup,
            skipPropertyNames: null,
            validatePotentialConversions: _strictAutoMapping,
            reader);

        var createInstance = CreateDefaultConstructor<T>();

        void AssignInstance(T instance, SqlDataReader r)
        {
            foreach (var (ordinal, setter) in mappings)
            {
                var value = r.IsDBNull(ordinal) ? null : r.GetValue(ordinal);
                setter(instance, value);
            }
        }

        return new MooMapPlan<T>(createInstance, AssignInstance);
    }

    private static Dictionary<string, int> CreateColumnLookup(SqlDataReader reader)
    {
        var columnLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < reader.FieldCount; i++)
        {
            columnLookup[reader.GetName(i)] = i;
        }

        return columnLookup;
    }

    private static PropertyInfo[] GetWritableProperties(Type type)
    {
        return type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0)
            .Where(p => p.SetMethod is not null && p.SetMethod.IsPublic)
            .ToArray();
    }

    private void ValidateStrictPropertyMapping(
        Type type,
        Dictionary<string, int> columnLookup,
        PropertyInfo[] writableProperties,
        SqlDataReader reader)
    {
        foreach (var property in writableProperties)
        {
            if (!columnLookup.TryGetValue(property.Name, out var ordinal))
            {
                throw new InvalidOperationException(
                    $"No matching column found for property '{property.Name}' on type '{type.Name}'.");
            }

            ValidatePotentialConversion(type, property.Name, reader.GetFieldType(ordinal), property.PropertyType);
        }

        foreach (var column in columnLookup.Keys)
        {
            if (!writableProperties.Any(p => string.Equals(p.Name, column, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    $"No matching property found for column '{column}' on type '{type.Name}'.");
            }
        }
    }

    private void ValidateStrictConstructorMapping(
        Type type,
        Dictionary<string, int> columnLookup,
        PropertyInfo[] writableProperties,
        HashSet<string> ctorParameterNames,
        SqlDataReader reader)
    {
        foreach (var property in writableProperties)
        {
            if (!ctorParameterNames.Contains(property.Name) && !columnLookup.TryGetValue(property.Name, out var ordinal))
            {
                throw new InvalidOperationException(
                    $"No matching column or constructor parameter found for property '{property.Name}' on type '{type.Name}'.");
            }

            if (columnLookup.TryGetValue(property.Name, out ordinal) && !ctorParameterNames.Contains(property.Name))
            {
                ValidatePotentialConversion(type, property.Name, reader.GetFieldType(ordinal), property.PropertyType);
            }
        }

        foreach (var column in columnLookup.Keys)
        {
            var matchesConstructorParameter = ctorParameterNames.Contains(column);
            var matchesWritableProperty = writableProperties.Any(
                p => string.Equals(p.Name, column, StringComparison.OrdinalIgnoreCase));

            if (!matchesConstructorParameter && !matchesWritableProperty)
            {
                throw new InvalidOperationException(
                    $"No matching constructor parameter or writable property found for column '{column}' on type '{type.Name}'.");
            }
        }
    }

    private void ValidatePotentialConversion(Type type, string memberName, Type sourceType, Type targetType)
    {
        if (!MooValueConverter.CanPotentiallyConvert(sourceType, targetType))
        {
            throw new InvalidOperationException(
                $"Column value for member '{memberName}' on type '{type.Name}' cannot be assigned from source type '{sourceType.Name}' to target type '{targetType.Name}'.");
        }
    }

    private List<(int Ordinal, Action<T, object?> Setter)> CreatePropertyMappings<T>(
        PropertyInfo[] writableProperties,
        Dictionary<string, int> columnLookup,
        HashSet<string>? skipPropertyNames,
        bool validatePotentialConversions,
        SqlDataReader reader)
    {
        var mappings = new List<(int Ordinal, Action<T, object?> Setter)>();

        foreach (var property in writableProperties)
        {
            if (skipPropertyNames is not null && skipPropertyNames.Contains(property.Name))
                continue;

            if (columnLookup.TryGetValue(property.Name, out var ordinal))
            {
                if (validatePotentialConversions)
                {
                    ValidatePotentialConversion(typeof(T), property.Name, reader.GetFieldType(ordinal), property.PropertyType);
                }

                var setter = CreateSetter<T>(property);
                mappings.Add((ordinal, setter));
            }
        }

        return mappings;
    }

    private ConstructorCandidate? FindBestConstructorCandidate<T>(SqlDataReader reader)
    {
        var type = typeof(T);
        var constructors = type.GetConstructors();
        var columnLookup = CreateColumnLookup(reader);
        ConstructorCandidate? bestCandidate = null;

        foreach (var ctor in constructors)
        {
            var parameters = ctor.GetParameters();

            if (parameters.Length == 0)
            {
                continue;
            }

            var score = 0;
            var parameterNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var isCandidate = true;

            foreach (var parameter in parameters)
            {
                if (parameter.Name is null || !columnLookup.TryGetValue(parameter.Name, out var ordinal))
                {
                    isCandidate = false;
                    break;
                }

                var fieldType = reader.GetFieldType(ordinal);

                if (!MooValueConverter.CanPotentiallyConvert(fieldType, parameter.ParameterType))
                {
                    isCandidate = false;
                    break;
                }

                parameterNames.Add(parameter.Name);
                score += GetConstructorMatchScore(fieldType, parameter.ParameterType);
            }

            if (!isCandidate)
            {
                continue;
            }

            var candidate = new ConstructorCandidate(ctor, parameterNames, score);

            if (bestCandidate is null
                || candidate.Score > bestCandidate.Score
                || (candidate.Score == bestCandidate.Score
                    && candidate.Constructor.GetParameters().Length > bestCandidate.Constructor.GetParameters().Length))
            {
                bestCandidate = candidate;
            }
        }

        return bestCandidate;
    }

    private static int GetConstructorMatchScore(Type sourceType, Type targetType)
    {
        var effectiveTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        var effectiveSourceType = Nullable.GetUnderlyingType(sourceType) ?? sourceType;

        if (effectiveTargetType == effectiveSourceType)
        {
            return 4;
        }

        if (effectiveTargetType.IsAssignableFrom(effectiveSourceType))
        {
            return 3;
        }

        if (effectiveTargetType.IsEnum && effectiveSourceType == typeof(string))
        {
            return 2;
        }

        return 1;
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

                args[i] = MooValueConverter.ConvertValue(value, parameters[i].ParameterType);
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
                typeof(MooValueConverter),
                nameof(MooValueConverter.ConvertValue),
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

    private sealed record ConstructorCandidate(
        ConstructorInfo Constructor,
        HashSet<string> ParameterNames,
        int Score);
}
