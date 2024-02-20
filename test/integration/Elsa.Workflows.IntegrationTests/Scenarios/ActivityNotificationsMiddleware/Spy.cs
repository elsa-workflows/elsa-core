namespace Elsa.Workflows.IntegrationTests.Scenarios.ActivityNotificationsMiddleware;

public class Spy
{
    public bool ActivityExecutingWasCalled { get; set; }
    public bool ActivityExecutedWasCalled { get; set; }
}