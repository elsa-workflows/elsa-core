using System.Collections.Concurrent;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Mediator.Services;

/// <inheritdoc />
public class JobQueue(IJobsChannel jobsChannel, ILogger<JobQueue> logger) : IJobQueue
{
    private readonly ConcurrentDictionary<string, EnqueuedJob> _scheduledItems = new();
    private readonly ConcurrentDictionary<string, EnqueuedJob> _pendingItems = new();

    /// <inheritdoc />
    public string Create(Func<CancellationToken, Task> job)
    {
        var jobItem = CreateJob(job);
        _pendingItems.TryAdd(jobItem.JobId, jobItem);
        return jobItem.JobId;
    }

    public void Enqueue(string jobId)
    {
        if (!_pendingItems.TryRemove(jobId, out var jobItem))
        {
            logger.LogWarning($"Job {jobId} was not found");
            return;
        }

        _scheduledItems.TryAdd(jobItem.JobId, jobItem);
        jobsChannel.Writer.TryWrite(jobItem);
    }

    /// <inheritdoc />
    public string Enqueue(Func<CancellationToken, Task> job)
    {
        var jobItem = CreateJob(job);
        _scheduledItems.TryAdd(jobItem.JobId, jobItem);
        jobsChannel.Writer.TryWrite(jobItem);
        return jobItem.JobId;
    }

    /// <inheritdoc />
    public bool Dequeue(string jobId)
    {
        if (!_pendingItems.TryRemove(jobId, out _))
            if (!_scheduledItems.TryRemove(jobId, out _))
                return false;

        return true;
    }

    /// <inheritdoc />
    public bool Cancel(string jobId)
    {
        if (!_pendingItems.TryGetValue(jobId, out var jobItem))
            if (!_scheduledItems.TryGetValue(jobId, out jobItem))
                return false;

        jobItem.CancellationTokenSource.Cancel();
        return true;
    }

    private EnqueuedJob CreateJob(Func<CancellationToken, Task> job)
    {
        var jobId = Guid.NewGuid().ToString();
        var cts = new CancellationTokenSource();
        return new EnqueuedJob(jobId, job, cts, OnJobCompleted);
    }

    void OnJobCompleted(string completedJobId) => _scheduledItems.TryRemove(completedJobId, out _);
}