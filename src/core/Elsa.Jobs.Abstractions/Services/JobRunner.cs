using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Jobs.Contracts;
using Elsa.Jobs.Models;

namespace Elsa.Jobs.Services;

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