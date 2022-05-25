using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Jobs.Models;

public class JobExecutionContext
{
    public JobExecutionContext(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        ServiceProvider = serviceProvider;
        CancellationToken = cancellationToken;
    }

    public IServiceProvider ServiceProvider { get; }
    public CancellationToken CancellationToken { get; }

    public T GetRequiredService<T>() where T : notnull => ServiceProvider.GetRequiredService<T>();
}