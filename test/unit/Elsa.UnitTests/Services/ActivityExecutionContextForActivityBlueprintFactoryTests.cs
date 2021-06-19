using System;
using System.Threading;
using Elsa.Services.Models;
using Elsa.Services.Workflows;
using Elsa.Testing.Shared;
using Elsa.Testing.Shared.AutoFixture.Attributes;
using Xunit;

namespace Elsa.Services
{
    public class ActivityExecutionContextForActivityBlueprintFactoryTests
    {
        [Theory(DisplayName = "The CreateActivityExecutionContext method should create a context using the activity blueprint, the workflow execution context, cancellation token and injected service provider."), AutoMoqData]
        public void CreateActivityExecutionContextCreatesContextUsingBlueprintExecutionContextCancellationTokenAndServiceProvider([WithAutofixtureResolution] IServiceProvider serviceProvider,
            IActivityBlueprint activityBlueprint,
            [OmitOnRecursion] WorkflowExecutionContext workflowExecutionContext,
            CancellationToken cancellationToken)
        {
            var sut = new ActivityExecutionContextForActivityBlueprintFactory(serviceProvider);

            var result = sut.CreateActivityExecutionContext(activityBlueprint, workflowExecutionContext, cancellationToken);

            Assert.True(ReferenceEquals(activityBlueprint, result.ActivityBlueprint), "The activity blueprint should be the same");
            Assert.True(ReferenceEquals(workflowExecutionContext, result.WorkflowExecutionContext), "The workflow execution context should be the same");
            Assert.True(Equals(cancellationToken, result.CancellationToken), "The cancellation token should be equal");
            Assert.True(ReferenceEquals(serviceProvider, result.ServiceProvider), "The service provider should be the same");
        }
    }
}