namespace MooDb;

internal static class MooValueConverter
{
    internal static T ConvertOrDefault<T>(object? value)
    {
        if (value is null || value is DBNull)
        {
            return default!;
        }

        return (T)ConvertValue(value, typeof(T))!;
    }

    internal static object? ConvertValue(object? value, Type targetType)
    {
        ArgumentNullException.ThrowIfNull(targetType);

        if (value is null || value is DBNull)
        {
            return null;
        }

        var effectiveTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (effectiveTargetType.IsInstanceOfType(value))
        {
            return value;
        }

        if (effectiveTargetType.IsEnum)
        {
            return ConvertEnum(value, effectiveTargetType);
        }

        if (effectiveTargetType == typeof(Guid))
        {
            return ConvertGuid(value);
        }

        if (effectiveTargetType == typeof(DateOnly))
        {
            return ConvertDateOnly(value);
        }

        if (effectiveTargetType == typeof(TimeOnly))
        {
            return ConvertTimeOnly(value);
        }

        return Convert.ChangeType(value, effectiveTargetType);
    }

    internal static bool CanPotentiallyConvert(Type sourceType, Type targetType)
    {
        ArgumentNullException.ThrowIfNull(sourceType);
        ArgumentNullException.ThrowIfNull(targetType);

        var effectiveTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        var effectiveSourceType = Nullable.GetUnderlyingType(sourceType) ?? sourceType;

        if (effectiveTargetType.IsAssignableFrom(effectiveSourceType))
        {
            return true;
        }

        if (effectiveTargetType.IsEnum)
        {
            return effectiveSourceType == typeof(string)
                || IsNumericType(effectiveSourceType);
        }

        if (effectiveTargetType == typeof(Guid))
        {
            return effectiveSourceType == typeof(string)
                || effectiveSourceType == typeof(byte[]);
        }

        if (effectiveTargetType == typeof(DateOnly))
        {
            return effectiveSourceType == typeof(DateTime)
                || effectiveSourceType == typeof(string);
        }

        if (effectiveTargetType == typeof(TimeOnly))
        {
            return effectiveSourceType == typeof(TimeSpan)
                || effectiveSourceType == typeof(DateTime)
                || effectiveSourceType == typeof(string);
        }

        return typeof(IConvertible).IsAssignableFrom(effectiveSourceType)
            && typeof(IConvertible).IsAssignableFrom(effectiveTargetType);
    }

    private static object ConvertEnum(object value, Type enumType)
    {
        if (value is string stringValue)
        {
            if (Enum.TryParse(enumType, stringValue, ignoreCase: true, out var parsed))
            {
                return parsed!;
            }

            var enumUnderlyingType = Enum.GetUnderlyingType(enumType);
            var convertedStringValue = Convert.ChangeType(stringValue, enumUnderlyingType);
            return Enum.ToObject(enumType, convertedStringValue!);
        }

        var underlyingType = Enum.GetUnderlyingType(enumType);
        var convertedValue = Convert.ChangeType(value, underlyingType);
        return Enum.ToObject(enumType, convertedValue!);
    }

    private static Guid ConvertGuid(object value)
    {
        return value switch
        {
            Guid guid => guid,
            string text => Guid.Parse(text),
            byte[] bytes when bytes.Length == 16 => new Guid(bytes),
            _ => throw new InvalidCastException(
                $"Value of type '{value.GetType().Name}' cannot be converted to '{typeof(Guid).Name}'.")
        };
    }

    private static DateOnly ConvertDateOnly(object value)
    {
        return value switch
        {
            DateOnly dateOnly => dateOnly,
            DateTime dateTime => DateOnly.FromDateTime(dateTime),
            string text => DateOnly.Parse(text),
            _ => throw new InvalidCastException(
                $"Value of type '{value.GetType().Name}' cannot be converted to '{typeof(DateOnly).Name}'.")
        };
    }

    private static TimeOnly ConvertTimeOnly(object value)
    {
        return value switch
        {
            TimeOnly timeOnly => timeOnly,
            TimeSpan timeSpan => TimeOnly.FromTimeSpan(timeSpan),
            DateTime dateTime => TimeOnly.FromDateTime(dateTime),
            string text => TimeOnly.Parse(text),
            _ => throw new InvalidCastException(
                $"Value of type '{value.GetType().Name}' cannot be converted to '{typeof(TimeOnly).Name}'.")
        };
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(byte)
            || type == typeof(sbyte)
            || type == typeof(short)
            || type == typeof(ushort)
            || type == typeof(int)
            || type == typeof(uint)
            || type == typeof(long)
            || type == typeof(ulong);
    }
}
