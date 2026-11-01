namespace Elsa.Workflows.IntegrationTests.Scenarios.ActivityNotificationsMiddleware;

public class Spy
{
    public bool ActivityExecutingWasCalled { get; set; }
    public bool ActivityExecutedWasCalled { get; set; }
    public bool ActivityFaultedWasCalled { get; set; }
    public Exception? FaultException { get; set; }
    public ActivityStatus? StatusWhenFaulted { get; set; }
}
