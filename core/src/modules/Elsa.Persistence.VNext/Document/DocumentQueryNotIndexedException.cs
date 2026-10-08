namespace Elsa.Persistence.VNext.Document;

public class DocumentQueryNotIndexedException(string storageUnit, IReadOnlyCollection<string> fields) : InvalidOperationException(
    $"No declared index exists for storage unit '{storageUnit}' and fields '{string.Join(", ", fields.Order())}'.")
{
    public string StorageUnit { get; } = storageUnit;
    public IReadOnlyCollection<string> Fields { get; } = fields;
}
