namespace Elsa.Mediator.Options;

public class MediatorOptions
{
    public int CommandWorkerCount { get; set; } = 4;
    public int NotificationWorkerCount { get; set; } = 4;
}