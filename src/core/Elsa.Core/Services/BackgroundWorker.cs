using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Elsa.Services
{
    public class BackgroundWorker : IBackgroundWorker
    {
        private readonly Channel<Func<ValueTask>> _channel;
        public BackgroundWorker() => _channel = Channel.CreateBounded<Func<ValueTask>>(10);

        public async Task ScheduleTask(Func<ValueTask> task, CancellationToken cancellationToken) => await _channel.Writer.WriteAsync(task, cancellationToken);

        public async Task ScheduleTask(Action task, CancellationToken cancellationToken) =>
            await _channel.Writer.WriteAsync(() =>
            {
                task();
                return new ValueTask();
            }, cancellationToken);

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            while (await _channel.Reader.WaitToReadAsync(cancellationToken))
            while (_channel.Reader.TryRead(out var task))
                await task();
        }
    }
}