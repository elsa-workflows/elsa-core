using Elsa.Jobs.Contracts;

namespace Elsa.Modules.Hangfire;

public class HangfireJobQueue : IJobQueue
{
    public Task SubmitJobAsync(IJob job, string? queueName = default, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}