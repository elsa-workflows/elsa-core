using Elsa.Workflows.Middleware.Activities;
using Xunit.Abstractions;

namespace Elsa.Workflows.Core.UnitTests.Middleware.Activities;

/// <summary>
/// Validates the behavior of the <see cref="DefaultActivityInvokerMiddleware"/> component by using the standard tests
/// from <see cref="ActivityInvokerMiddlewareTestsBase{T}"/>. 
/// </summary>
public class DefaultActivityInvokerMiddlewareTests : ActivityInvokerMiddlewareTestsBase<DefaultActivityInvokerMiddleware>
{
    public DefaultActivityInvokerMiddlewareTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {

    }
}
