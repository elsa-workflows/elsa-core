using Elsa.Expressions.Contracts;
using Elsa.JavaScript.Handlers;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.Contracts;
using Moq;
using Xunit;

namespace Elsa.JavaScript.UnitTests;

public class WorkflowDefinitionActivityJavaScriptHandlerTests
{
    private readonly WorkflowDefinitionActivityJavaScriptHandler _handler;

    public WorkflowDefinitionActivityJavaScriptHandlerTests()
    {
        var activityRegistryMock = new Mock<IActivityRegistry>();
        var expressionEvaluatorMock = new Mock<IExpressionEvaluator>();
        _handler = new WorkflowDefinitionActivityJavaScriptHandler(activityRegistryMock.Object, expressionEvaluatorMock.Object);
    }
    
    [Fact]
    public void Test1()
    {
        
    }
    
}