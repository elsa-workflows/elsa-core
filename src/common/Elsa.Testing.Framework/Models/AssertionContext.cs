using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Testing.Framework.Models;

public class AssertionContext
{
    public RunWorkflowResult RunWorkflowResult { get; init; } = null!;
    public CancellationToken CancellationToken { get; init; } = CancellationToken.None;
    public IServiceProvider ServiceProvider { get; init; } = null!;

    public T GetRequiredService<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }
}