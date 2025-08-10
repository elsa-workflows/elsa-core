using Elsa.Testing.Framework.Models;

namespace Elsa.Testing.Framework.Abstractions;

public abstract class Assertion
{
    public string Id { get; set; } = null!;
    public virtual Task BeforeRunAsync(AssertionContext context)
    {
        // Default implementation does nothing.
        return Task.CompletedTask;
    }
    public abstract Task<AssertionResult> RunAsync(AssertionContext context);
}