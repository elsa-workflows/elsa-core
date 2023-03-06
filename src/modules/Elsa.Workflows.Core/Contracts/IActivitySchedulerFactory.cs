namespace Elsa.Workflows.Core.Contracts;

public interface IActivitySchedulerFactory
{
    IActivityScheduler CreateScheduler();
}