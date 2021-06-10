using System.Collections.Generic;
using System.Linq;
using Elsa.Services.Models;
using Elsa.Services.Workflows;
using Elsa.Testing.Shared;
using Moq;
using Xunit;

namespace Elsa.Services
{
    public class StartActivitiesForCompositeActivityBlueprintProviderTests
    {
        [Theory(DisplayName = "The GetStartActivities returns only activities that have no inbound workflow connections"), AutoMoqData]
        public void GetStartActivitiesReturnsAllActivitiesWhichHaveNoInboundConnections(GetsStartActivitiesProvider sut,
                                                                                        IWorkflowBlueprint workflowBlueprint,
                                                                                        IActivityBlueprint activityBlueprint1,
                                                                                        IActivityBlueprint activityBlueprint2,
                                                                                        IActivityBlueprint activityBlueprint3,
                                                                                        IActivityBlueprint activityBlueprint4,
                                                                                        string activityBlueprintId1,
                                                                                        string activityBlueprintId2,
                                                                                        string activityBlueprintId3,
                                                                                        string activityBlueprintId4,
                                                                                        IConnection connection1,
                                                                                        IConnection connection2,
                                                                                        ITargetEndpoint endpoint1,
                                                                                        ITargetEndpoint endpoint2)
        {
            SetupActivitiesWithIds(new () {
                {activityBlueprintId1, activityBlueprint1},
                {activityBlueprintId2, activityBlueprint2},
                {activityBlueprintId3, activityBlueprint3},
                {activityBlueprintId4, activityBlueprint4},
            });
            Mock.Get(workflowBlueprint).SetupGet(x => x.Connections).Returns(new [] { connection1, connection2 });
            Mock.Get(workflowBlueprint).SetupGet(x => x.Activities).Returns(new [] { activityBlueprint1, activityBlueprint2, activityBlueprint3, activityBlueprint4 });
            Mock.Get(connection1).SetupGet(x => x.Target).Returns(endpoint1);
            Mock.Get(connection2).SetupGet(x => x.Target).Returns(endpoint2);
            Mock.Get(endpoint1).SetupGet(x => x.Activity).Returns(activityBlueprint2);
            Mock.Get(endpoint2).SetupGet(x => x.Activity).Returns(activityBlueprint4);

            var result = sut.GetStartActivities(workflowBlueprint).ToArray();

            Assert.Equal(new [] { activityBlueprint1, activityBlueprint3 }, result);
        }

        void SetupActivitiesWithIds(Dictionary<string,IActivityBlueprint> idsToBlueprints)
        {
            foreach(var kvp in idsToBlueprints)
                Mock.Get(kvp.Value).SetupGet(x => x.Id).Returns(kvp.Key);
        }
    }
}