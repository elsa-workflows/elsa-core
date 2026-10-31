using Elsa.Workflows.Runtime.ProtoActor.Mappers;

namespace Elsa.Workflows.Runtime.ProtoActor.UnitTests.Mappers;

public class WorkflowSubStatusMapperTests
{
    [Fact]
    public void Map_RoundTripsEverySubStatus()
    {
        var mapper = new WorkflowSubStatusMapper();

        foreach (var subStatus in Enum.GetValues<WorkflowSubStatus>())
        {
            var proto = mapper.Map(subStatus);
            Assert.Equal(subStatus, mapper.Map(proto));
        }
    }
}
