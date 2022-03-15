using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Jobs.Models;

public class JobExecutionContext
{
    public JobExecutionContext(IServiceProvider serviceProvider, CancellationToken cancellation)
    {
        ServiceProvider = serviceProvider;
        Cancellation = cancellation;
    }

    public IServiceProvider ServiceProvider { get; }
    public CancellationToken Cancellation { get; }

    public T GetRequiredService<T>() where T : notnull => ServiceProvider.GetRequiredService<T>();
}