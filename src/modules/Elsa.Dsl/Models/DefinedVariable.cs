namespace Elsa.Dsl.Models;

public class DefinedVariable
{
    public string Identifier { get; set; } = default!;
    public Type Type { get; set; } = default!;
    public object? Value { get; set; }
}