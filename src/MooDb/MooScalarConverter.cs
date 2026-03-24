namespace MooDb;

internal static class MooScalarConverter
{
    internal static T ConvertRequired<T>(object? value)
    {
        if (value is null)
        {
            throw new InvalidOperationException("The scalar query returned no rows.");
        }

        if (value is DBNull)
        {
            throw new InvalidOperationException("The scalar query returned DBNull.");
        }

        return ConvertValue<T>(value);
    }

    internal static T? ConvertOrDefault<T>(object? value)
    {
        if (value is null || value is DBNull)
        {
            return default;
        }

        return ConvertValue<T>(value);
    }

    private static T ConvertValue<T>(object value)
    {
        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        if (targetType.IsInstanceOfType(value))
        {
            return (T)value;
        }

        return (T)Convert.ChangeType(value, targetType);
    }
}
