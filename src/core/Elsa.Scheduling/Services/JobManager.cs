using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Scheduling.Contracts;

namespace Elsa.Scheduling.Services;

public class JobManager : IJobManager
{
    private readonly IEnumerable<IJobHandler> _handlers;

    public JobManager(IEnumerable<IJobHandler> handlers)
    {
        _handlers = handlers;
    }

    public async Task ExecuteJobAsync(IJob job, CancellationToken cancellationToken = default)
    {
        var handler = _handlers.FirstOrDefault(x => x.Supports(job));

        if (handler == null)
            throw new NotSupportedException($"The specified job of type {job.GetType().Name} does not have a handler");

        await handler.HandleAsync(job, cancellationToken);
    }
}