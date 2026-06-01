namespace Elsa.ModularPersistence.Documents;

internal static class DocumentValidation
{
    public static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be empty.", parameterName);

        return value.Trim();
    }
}
