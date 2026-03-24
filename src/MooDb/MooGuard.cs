namespace MooDb;

internal static class MooGuard
{
    internal static void AgainstNullOrWhiteSpace(string? value, string paramName, string displayName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{displayName} cannot be null, empty, or whitespace.", paramName);
        }
    }
}
