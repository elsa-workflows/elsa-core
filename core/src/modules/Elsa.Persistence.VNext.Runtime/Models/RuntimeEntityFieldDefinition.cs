namespace Elsa.Persistence.VNext.Runtime.Models;

public record RuntimeEntityFieldDefinition(
    string Name,
    RuntimeEntityFieldType Type,
    bool IsRequired = false);
