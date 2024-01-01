namespace Elsa.Workflows.Contracts;

public interface IActivitySchedulerFactory
{
    IActivityScheduler CreateScheduler();
}