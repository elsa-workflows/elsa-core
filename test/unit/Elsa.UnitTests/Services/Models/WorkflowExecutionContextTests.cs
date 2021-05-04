using Xunit;
using Elsa.Testing.Shared.AutoFixture.Attributes;
using AutoFixture.Xunit2;
using System;
using System.Threading;
using Elsa.Testing.Shared;

namespace Elsa.Services.Models
{
    public class WorkflowExecutionContextTests
    {
        [Theory(DisplayName = "The PurgeVariables method should clear the Variables instance associated with the WorkflowInstance"), AutoMoqData]
        public void PurgeVariables_clears_workflow_execution_context_workflow_instance_variables([WithAutofixtureResolution, Frozen] IServiceProvider serviceProvider,
            [OmitOnRecursion] WorkflowExecutionContext workflowExecutionContext,
            IActivityBlueprint activityBlueprint,
            CancellationToken cancellationToken,
            string variableName,
            object variableValue)
        {
            var sut = new ActivityExecutionContext(serviceProvider, workflowExecutionContext, activityBlueprint, null, false, cancellationToken);
            sut.WorkflowInstance.Variables.Set(variableName, variableValue);

            sut.PurgeVariables();

            Assert.Empty(sut.WorkflowInstance.Variables.Data);
        }
    }
}