using System;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Services
{
    public interface IBackgroundWorker
    {
        Task ScheduleTask(Func<ValueTask> task, CancellationToken cancellationToken = default);
        Task ScheduleTask(Action task, CancellationToken cancellationToken = default);
        Task RunAsync(CancellationToken cancellationToken = default);
    }
}