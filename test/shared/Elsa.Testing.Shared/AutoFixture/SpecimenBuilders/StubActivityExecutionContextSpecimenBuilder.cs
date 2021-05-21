using System;
using AutoFixture;
using AutoFixture.Kernel;
using Elsa.Models;
using Elsa.Services.Models;
using Moq;

namespace Elsa.Testing.Shared.AutoFixture.SpecimenBuilders
{
    public class StubActivityExecutionContextSpecimenBuilder : ISpecimenBuilder
    {
        public object Create(object request, ISpecimenContext context)
        {
            if(!request.IsAnAutofixtureRequestForType<ActivityExecutionContext>())
                return new NoSpecimen();

            var serviceProvider = GetServiceProvider(context);
            var workflowExecutionContext = GetWorkflowExecutionContext(context, serviceProvider);
            var activityBlueprint = Mock.Of<IActivityBlueprint>();

            Mock.Get(activityBlueprint).SetupGet(x => x.Id).Returns(context.Create<string>());
            
            return new ActivityExecutionContext(serviceProvider,
                                                workflowExecutionContext,
                                                activityBlueprint,
                                                default,
                                                default,
                                                default);
        }

        static IServiceProvider GetServiceProvider(ISpecimenContext context)
            => (IServiceProvider) new AutofixtureResolutionServiceProviderSpecimenBuilder().Create(typeof(IServiceProvider), context);

        static WorkflowExecutionContext GetWorkflowExecutionContext(ISpecimenContext context, IServiceProvider serviceProvider)
            => new WorkflowExecutionContext(serviceProvider, Mock.Of<IWorkflowBlueprint>(), new WorkflowInstance());
    }
}