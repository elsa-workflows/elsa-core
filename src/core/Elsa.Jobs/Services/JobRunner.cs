using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Jobs.Contracts;

namespace Elsa.Jobs.Services;

public class JobRunner : IJobRunner
{
    private readonly IEnumerable<IJobHandler> _handlers;

    public JobRunner(IEnumerable<IJobHandler> handlers)
    {
        _handlers = handlers;
    }

    public async Task RunJobAsync(IJob job, CancellationToken cancellationToken = default)
    {
        var handler = _handlers.FirstOrDefault(x => x.GetSupports(job));

        if (handler == null)
            throw new NotSupportedException($"The specified job of type {job.GetType().Name} does not have a handler");

        await handler.HandleAsync(job, cancellationToken);
    }
}