using Elsa.ModularPersistence.Validation;

namespace Elsa.ModularPersistence.Sqlite;

public static class SqliteDocumentProviderCapabilities
{
    public static ProviderCapabilities Value { get; } = ProviderCapabilities.PortableDocument;
}
