using System.Threading;
using System.Threading.Tasks;
using Quartz;

namespace Elsa.Activities.Temporal.Quartz.Services
{
    public class QuartzSchedulerProvider
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly SemaphoreSlim _semaphore = new(1);
        private IScheduler? _scheduler;

        public QuartzSchedulerProvider(ISchedulerFactory schedulerFactory) => _schedulerFactory = schedulerFactory;

        public async Task<IScheduler> GetSchedulerAsync(CancellationToken cancellationToken)
        {
            if (_scheduler != null)
                return _scheduler;
            
            await _semaphore.WaitAsync(cancellationToken);
            
            try
            {
                if (_scheduler != null)
                    return _scheduler;
                
                _scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
                return _scheduler!;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}