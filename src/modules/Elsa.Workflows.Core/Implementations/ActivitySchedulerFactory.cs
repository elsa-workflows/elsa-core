using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Implementations;

public class ActivitySchedulerFactory : IActivitySchedulerFactory
{
    //public IActivityScheduler CreateScheduler() => new StackBasedActivityScheduler();
    public IActivityScheduler CreateScheduler() => new QueueBasedActivityScheduler();
}