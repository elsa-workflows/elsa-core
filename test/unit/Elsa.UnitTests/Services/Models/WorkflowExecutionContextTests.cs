using Xunit;
using Elsa.Testing.Shared.AutoFixture.Attributes;
using AutoFixture.Xunit2;
using System;

namespace Elsa.Services.Models
{
    public class WorkflowExecutionContextTests
    {
        [Theory(DisplayName = "The PurgeVariables method should clear the Variables instance associated with the WorkflowInstance"), AutoMoqData]
        public void PurgeVariables_clears_workflow_execution_context_workflow_instance_variables([AutofixtureServiceProvider, Frozen] IServiceProvider serviceProvider,
                                                                                                 [OmitOnRecursion,NoAutoProperties] WorkflowExecutionContext sut,
                                                                                                 string variableName,
                                                                                                 object variableValue)
        {
            sut.WorkflowInstance.Variables.Set(variableName, variableValue);

            sut.PurgeVariables();

            Assert.Empty(sut.WorkflowInstance.Variables.Data);
        }
    }
}