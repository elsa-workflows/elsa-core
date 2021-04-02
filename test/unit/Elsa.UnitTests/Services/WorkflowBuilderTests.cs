using System;
using AutoFixture.Xunit2;
using Elsa.Builders;
using Elsa.Models;
using Elsa.Testing.Shared.AutoFixture.Attributes;
using Xunit;

namespace Elsa.Services
{
    public class WorkflowBuilderTests
    {
        [Theory(DisplayName = "The Build method should return a workflow blueprint with the WorkflowBurst persistence behaviour if no behaviour was specified"), AutoMoqData]
        public void BuildShouldReturnWorkflowBlueprintWithWorkflowBurstPersistenceBehaviourIfNoBehaviourSpecified([WithAutofixtureResolution, Frozen] IServiceProvider serviceProvider,
                                                                                                                  WorkflowBuilder sut,
                                                                                                                  string idPrefix)
        {
            var workflow = new NoOpWorkflow();

            var blueprint = sut.Build(workflow, idPrefix);

            Assert.Equal(WorkflowPersistenceBehavior.WorkflowBurst, blueprint.PersistenceBehavior);
        }

        class NoOpWorkflow : IWorkflow
        {
            public void Build(IWorkflowBuilder builder) { }
        }
    }
}