using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Jobs.Models;
using Elsa.Jobs.Services;

namespace Elsa.Jobs.Implementations;

public class JobRunner : IJobRunner
{
    private readonly IServiceProvider _serviceProvider;

    public JobRunner(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task RunJobAsync(IJob job, CancellationToken cancellationToken = default)
    {
        var context = new JobExecutionContext(_serviceProvider, cancellationToken);
        await job.ExecuteAsync(context);
    }
}