namespace Elsa.Workflows.Core.Services;

public interface IActivitySchedulerFactory
{
    IActivityScheduler CreateScheduler();
}