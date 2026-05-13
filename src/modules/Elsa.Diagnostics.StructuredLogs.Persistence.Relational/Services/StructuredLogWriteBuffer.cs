using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Contracts;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Options;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Stores;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Services;

public class StructuredLogWriteBuffer(
    RelationalStructuredLogStore store,
    IOptions<RelationalStructuredLogOptions> options) : IStructuredLogStore, IStructuredLogWriteBuffer, IHostedService, IAsyncDisposable
{
    private readonly Queue<StructuredLogEvent> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);
    private readonly CancellationTokenSource _stopTokenSource = new();
    private Task? _backgroundTask;
    private long _droppedWriteCount;
    private int _disposed;

    public long DroppedWriteCount => Interlocked.Read(ref _droppedWriteCount);

    public ValueTask WriteAsync(StructuredLogEvent logEvent, CancellationToken cancellationToken = default)
    {
        lock (_queue)
        {
            if (_queue.Count >= Math.Max(1, options.Value.WriteQueue.Capacity))
            {
                Interlocked.Increment(ref _droppedWriteCount);
                return ValueTask.CompletedTask;
            }

            _queue.Enqueue(logEvent);
        }

        _signal.Release();
        return ValueTask.CompletedTask;
    }

    public async ValueTask WriteManyAsync(IReadOnlyCollection<StructuredLogEvent> logEvents, CancellationToken cancellationToken = default)
    {
        foreach (var logEvent in logEvents)
            await WriteAsync(logEvent, cancellationToken);
    }

    public ValueTask<RecentStructuredLogsResult> QueryAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default)
    {
        return store.QueryAsync(filter, cancellationToken);
    }

    public ValueTask<IReadOnlyCollection<StructuredLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default)
    {
        return store.ListSourcesAsync(cancellationToken);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _backgroundTask ??= Task.Run(ProcessQueueAsync, CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _stopTokenSource.CancelAsync();

        if (_backgroundTask != null)
        {
            try
            {
                await _backgroundTask.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
        }

        using var timeoutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutTokenSource.CancelAfter(options.Value.WriteQueue.ShutdownFlushTimeout);
        try
        {
            await FlushAsync(timeoutTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    public async ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            var batch = DequeueBatch();
            if (batch.Count == 0)
                return;

            await store.WriteManyAsync(batch, cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        await _stopTokenSource.CancelAsync();
        _signal.Dispose();
        _stopTokenSource.Dispose();
    }

    private async Task ProcessQueueAsync()
    {
        using var timer = new PeriodicTimer(options.Value.WriteQueue.FlushInterval);
        var cancellationToken = _stopTokenSource.Token;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var signalTask = _signal.WaitAsync(cancellationToken);
                var timerTask = timer.WaitForNextTickAsync(cancellationToken).AsTask();
                await Task.WhenAny(signalTask, timerTask);
                await FlushAsync(CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception e)
            {
                _ = e;
            }
        }
    }

    private IReadOnlyCollection<StructuredLogEvent> DequeueBatch()
    {
        var batchSize = Math.Max(1, options.Value.WriteQueue.BatchSize);
        var batch = new List<StructuredLogEvent>(batchSize);

        lock (_queue)
        {
            while (_queue.Count > 0 && batch.Count < batchSize)
                batch.Add(_queue.Dequeue());
        }

        return batch;
    }
}
