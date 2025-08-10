namespace Elsa.Testing.Framework.Models;

public class AssertionResult
{
    public string AssertionId { get; set; } = null!;
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
}