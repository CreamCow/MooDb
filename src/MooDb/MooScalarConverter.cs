namespace MooDb;

internal static class MooScalarConverter
{
    internal static T ConvertScalarOrDefault<T>(object? value)
    {
        return MooValueConverter.ConvertOrDefault<T>(value);
    }
}
