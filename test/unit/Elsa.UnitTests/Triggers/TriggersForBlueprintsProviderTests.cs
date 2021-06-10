using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Elsa.Services.Triggers;
using Elsa.Testing.Shared;
using Elsa.Testing.Shared.AutoFixture.Attributes;
using Moq;
using Xunit;

namespace Elsa.Triggers
{
    public class TriggersForBlueprintsProviderTests
    {
        [Theory(DisplayName = "The GetTriggersAsync method should return all triggers for start activities of the workflow blueprints"), AutoMoqData]
        public async Task GetTriggersAsyncGetsAllTriggersForAllBlueprintsStartActivitiesAndCompatibleBookmarks([Frozen] IActivityTypeService activityTypeService,
                                                                                                               [Frozen] ICreatesWorkflowExecutionContextForWorkflowBlueprint workflowExecutionContextFactory,
                                                                                                               [Frozen] IGetsTriggersForActivityBlueprintAndWorkflow triggerProvider,
                                                                                                               [Frozen] IGetsStartActivities startingActivitiesProvider,
                                                                                                               TriggersForBlueprintsProvider sut,
                                                                                                               IWorkflowBlueprint workflowBlueprint1,
                                                                                                               IWorkflowBlueprint workflowBlueprint2,
                                                                                                               ActivityType activityType1,
                                                                                                               ActivityType activityType2,
                                                                                                               ActivityType activityType3,
                                                                                                               IActivityBlueprint activityBlueprint1,
                                                                                                               IActivityBlueprint activityBlueprint2,
                                                                                                               IActivityBlueprint activityBlueprint3,
                                                                                                               WorkflowTrigger trigger1,
                                                                                                               WorkflowTrigger trigger2,
                                                                                                               WorkflowTrigger trigger3,
                                                                                                               WorkflowTrigger trigger4,
                                                                                                               WorkflowTrigger trigger5,
                                                                                                               WorkflowTrigger trigger6,
                                                                                                               [WithAutofixtureResolution] IServiceProvider serviceProvider,
                                                                                                               [NoAutoProperties] WorkflowInstance workflowInstance)
        {
            Mock.Get(activityTypeService)
                .Setup(x => x.GetActivityTypesAsync(default))
                .Returns(ValueTask.FromResult<IEnumerable<ActivityType>>(new [] { activityType1, activityType2, activityType3 }));
            Mock.Get(startingActivitiesProvider)
                .Setup(x => x.GetStartActivities(workflowBlueprint1))
                .Returns(() => new [] { activityBlueprint1, activityBlueprint2 });
            Mock.Get(startingActivitiesProvider)
                .Setup(x => x.GetStartActivities(workflowBlueprint2))
                .Returns(() => new [] { activityBlueprint3 });
            Mock.Get(workflowExecutionContextFactory)
                .Setup(x => x.CreateWorkflowExecutionContextAsync(It.IsAny<IWorkflowBlueprint>(), default))
                .Returns((IWorkflowBlueprint bp, CancellationToken c) => Task.FromResult(new WorkflowExecutionContext(serviceProvider, bp, workflowInstance, default)));
            Mock.Get(triggerProvider)
                .Setup(x => x.GetTriggersForActivityBlueprintAsync(activityBlueprint1, It.IsAny<WorkflowExecutionContext>(), It.IsAny<IDictionary<string,ActivityType>>(), default))
                .Returns(() => Task.FromResult<IEnumerable<WorkflowTrigger>>(new [] { trigger1, trigger2 }));
            Mock.Get(triggerProvider)
                .Setup(x => x.GetTriggersForActivityBlueprintAsync(activityBlueprint2, It.IsAny<WorkflowExecutionContext>(), It.IsAny<IDictionary<string,ActivityType>>(), default))
                .Returns(() => Task.FromResult<IEnumerable<WorkflowTrigger>>(new [] { trigger3, trigger4 }));
            Mock.Get(triggerProvider)
                .Setup(x => x.GetTriggersForActivityBlueprintAsync(activityBlueprint3, It.IsAny<WorkflowExecutionContext>(), It.IsAny<IDictionary<string,ActivityType>>(), default))
                .Returns(() => Task.FromResult<IEnumerable<WorkflowTrigger>>(new [] { trigger5, trigger6 }));

            var results = await sut.GetTriggersAsync(new [] { workflowBlueprint1, workflowBlueprint2 });

            Assert.Equal(new [] { trigger1, trigger2, trigger3, trigger4, trigger5, trigger6 },
                         results);
        }
    }
}