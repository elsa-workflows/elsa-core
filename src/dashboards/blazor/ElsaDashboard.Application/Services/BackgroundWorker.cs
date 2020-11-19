using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ElsaDashboard.Application.Services
{
    public class BackgroundWorker
    {
        private readonly Channel<Func<ValueTask>> _channel;

        public BackgroundWorker()
        {
            _channel = Channel.CreateBounded<Func<ValueTask>>(10);
        }

        public async Task ScheduleTask(Func<ValueTask> task, CancellationToken cancellationToken = default) => await _channel.Writer.WriteAsync(task, cancellationToken);

        public async Task ScheduleTask(Action task, CancellationToken cancellationToken = default) =>
            await _channel.Writer.WriteAsync(() =>
            {
                task();
                return ValueTask.CompletedTask;
            }, cancellationToken);

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            while (await _channel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (_channel.Reader.TryRead(out var task))
                {
                    await task();
                }
            }
        }
    }
}