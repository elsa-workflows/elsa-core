using Xunit;
using Elsa.Testing.Shared.AutoFixture.Attributes;
using AutoFixture.Xunit2;
using System;

namespace Elsa.Services.Models
{
    public class ActivityExecutionContextTests
    {
        [Theory(DisplayName = "The PurgeVariables method should clear the Variables instance associated with the WorkflowInstance associated with the Workflow Execution Context"), AutoMoqData]
        public void PurgeVariables_clears_workflow_execution_context_workflow_instance_variables([AutofixtureServiceProvider, Frozen] IServiceProvider serviceProvider,
                                                                                                 [OmitOnRecursion,NoAutoProperties] ActivityExecutionContext sut,
                                                                                                 string variableName,
                                                                                                 object variableValue)
        {
            sut.WorkflowExecutionContext.WorkflowInstance.Variables.Set(variableName, variableValue);

            sut.PurgeVariables();

            Assert.Empty(sut.WorkflowExecutionContext.WorkflowInstance.Variables.Data);
        }
    }
}