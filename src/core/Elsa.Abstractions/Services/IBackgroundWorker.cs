using System;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;

namespace Elsa.Services
{
    public interface IBackgroundWorker
    {
        Task ScheduleTask(string channelName, Func<ValueTask> task, CancellationToken cancellationToken = default, Duration? channelTtl = default);
        Task ScheduleTask(string channelName, Action task, CancellationToken cancellationToken = default, Duration? channelTtl = default);
    }
}