using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Services;

public class ActivitySchedulerFactory : IActivitySchedulerFactory
{
    //public IActivityScheduler CreateScheduler() => new StackBasedActivityScheduler();
    public IActivityScheduler CreateScheduler() => new QueueBasedActivityScheduler();
}