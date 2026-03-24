namespace MooDb;

internal static class MooErrorMessages
{
    internal const string NoMoreResultSetsAvailable = "No more result sets are available.";
    internal const string ExpectedSingleRow = "Expected at most one row but received more than one.";

    internal static string ParameterNotFound(string name) =>
        $"No parameter named '{name}' exists in this collection.";

    internal static string ParameterHasNoValue(string name) =>
        $"Parameter '{name}' does not contain a value.";

    internal static string ParameterTypeMismatch(string name, string actualTypeName, string expectedTypeName) =>
        $"Parameter '{name}' contains a value of type '{actualTypeName}', which cannot be read as '{expectedTypeName}'.";

    internal static string DuplicateParameter(string name) =>
        $"A parameter named '{name}' has already been added.";
}
