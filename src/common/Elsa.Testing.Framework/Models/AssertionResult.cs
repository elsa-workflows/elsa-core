namespace Elsa.Testing.Framework.Models;

public class AssertionResult
{
    public bool Passed { get; set; }
    public string? ErrorMessage { get; set; }

    public static AssertionResult Pass() => new()
    {
        Passed = true
    };

    public static AssertionResult Fail(string errorMessage) => new()
    {
        Passed = false,
        ErrorMessage = errorMessage
    };
}