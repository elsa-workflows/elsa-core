using System.Collections.Concurrent;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;

namespace Elsa.Mediator.Services;

/// <inheritdoc />
public class JobQueue : IJobQueue
{
    private readonly IJobsChannel _jobsChannel;
    private readonly ConcurrentDictionary<string, EnqueuedJob> _jobItems;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobQueue"/> class.
    /// </summary>
    public JobQueue(IJobsChannel jobsChannel)
    {
        _jobsChannel = jobsChannel;
        _jobItems = new ConcurrentDictionary<string, EnqueuedJob>();
    }

    /// <inheritdoc />
    public string Enqueue(Func<CancellationToken, Task> job)
    {
        var jobId = Guid.NewGuid().ToString();
        var cts = new CancellationTokenSource();
        var jobItem = new EnqueuedJob(jobId, job, cts, OnJobCompleted);
        
        _jobItems.TryAdd(jobId, jobItem);
        _jobsChannel.Writer.TryWrite(jobItem);
        return jobId;
    }

    /// <inheritdoc />
    public bool Cancel(string jobId)
    {
        if (!_jobItems.TryGetValue(jobId, out var jobItem)) 
            return false;
        
        jobItem.CancellationTokenSource.Cancel();
        return true;

    }
    
    void OnJobCompleted(string completedJobId) => _jobItems.TryRemove(completedJobId, out _);
}