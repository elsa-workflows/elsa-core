using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using NodaTime;

namespace Elsa.Services
{
    public class BackgroundWorker : IBackgroundWorker
    {
        private readonly IDictionary<string, Channel<Func<ValueTask>>> _channels = new Dictionary<string, Channel<Func<ValueTask>>>();
        private readonly SemaphoreSlim _semaphore = new(1);

        public async Task ScheduleTask(string channelName, Func<ValueTask> task, CancellationToken cancellationToken, Duration? channelTtl)
        {
            var channel = await GetOrCreateChannel(channelName, channelTtl, cancellationToken);
            await channel.Writer.WriteAsync(task, cancellationToken);
        }

        public async Task ScheduleTask(string channelName, Action task, CancellationToken cancellationToken, Duration? channelTtl)
        {
            await ScheduleTask(channelName, () =>
            {
                task();
                return new ValueTask();
            }, cancellationToken, channelTtl);
        }

        private async Task<Channel<Func<ValueTask>>> GetOrCreateChannel(string channelName, Duration? channelTtl, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                var channel = _channels.ContainsKey(channelName) ? _channels[channelName] : default;

                if (channel != null)
                    return channel;

                channel = Channel.CreateBounded<Func<ValueTask>>(10);
                _channels[channelName] = channel;
                var reader = new BackgroundChannelReader(channel, () => CompleteChannel(channelName));
                reader.Start(channelTtl, cancellationToken);

                return channel;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private void CompleteChannel(string name)
        {
            if (_channels.ContainsKey(name))
                _channels.Remove(name);
        }

        private class BackgroundChannelReader
        {
            private readonly Channel<Func<ValueTask>> _channel;
            private readonly Action _onComplete;

            public BackgroundChannelReader(Channel<Func<ValueTask>> channel, Action onComplete)
            {
                _channel = channel;
                _onComplete = onComplete;
            }

            public void Start(Duration? channelTtl, CancellationToken cancellationToken) => Task.Factory.StartNew(() => ProcessAsync(channelTtl, cancellationToken), cancellationToken);
            
            private async Task ProcessAsync(Duration? channelTtl, CancellationToken cancellationToken)
            {
                var timeOutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                if (channelTtl != null) 
                    timeOutTokenSource.CancelAfter(channelTtl.Value.ToTimeSpan());

                try
                {
                    while (await _channel.Reader.WaitToReadAsync(timeOutTokenSource.Token))
                    {
                        var task = await _channel.Reader.ReadAsync(CancellationToken.None);
                        await task();

                        if (channelTtl != null) 
                            timeOutTokenSource.CancelAfter(channelTtl.Value.ToTimeSpan()); // Reset timeout.
                    }
                }
                catch(TaskCanceledException)
                {
                    _onComplete();
                }
            }
        }
    }
}