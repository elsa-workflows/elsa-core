using Elsa.Workflows.IncidentStrategies;

namespace Elsa.Workflows.IntegrationTests.Scenarios.Incidents.Statics;

public static class TestSettings
{
    public static Type IncidentStrategyType { get; set; } = typeof(FaultStrategy);
}