using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using Elsa.Testing.Shared.Autofixture;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;
using NodaTime.Testing;
using Xunit;

namespace Elsa.Core.UnitTests
{
    public class WorkflowHostTests
    {
        private readonly IFixture fixture;
        private readonly WorkflowHost workflowHost;

        public WorkflowHostTests()
        {
            fixture = new Fixture().Customize(new NodaTimeCustomization());
            var workflowActivatorMock = new Mock<IWorkflowActivator>();
            var idGeneratorMock = new Mock<IIdGenerator>();
            var workflowRegistryMock = new Mock<IWorkflowRegistry>();
            var workflowInstanceStoreMock = new Mock<IWorkflowInstanceStore>();
            var workflowExpressionEvaluatorMock = new Mock<IExpressionEvaluator>();
            var mediatorMock = new Mock<IMediator>();
            var now = fixture.Create<Instant>();
            var clock = new FakeClock(now);
            var logger = new NullLogger<WorkflowHost>();
            var serviceProvider = new ServiceCollection().BuildServiceProvider();

            workflowActivatorMock
                .Setup(x => x.ActivateAsync(It.IsAny<Workflow>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Workflow workflow, string? correlationId, CancellationToken cancellationToken) => new WorkflowInstance());

            workflowHost = new WorkflowHost(
                workflowRegistryMock.Object,
                workflowInstanceStoreMock.Object,
                workflowActivatorMock.Object,
                workflowExpressionEvaluatorMock.Object,
                clock,
                mediatorMock.Object,
                serviceProvider,
                logger);
        }

        [Fact(DisplayName = "Can run simple workflow to completed state.")]
        public async Task RunAsync01()
        {
            var activityExecutionResultMock = new Mock<IActivityExecutionResult>();
            var activity = CreateActivity(activityExecutionResult: activityExecutionResultMock.Object);
            var workflow = CreateWorkflow(activity);
            var executionContext = await workflowHost.RunWorkflowAsync(workflow);

            Assert.Equal(WorkflowStatus.Completed, executionContext.CreateWorkflowInstance().Status);
        }

        [Fact(DisplayName = "Invokes returned activity execution result.")]
        public async Task RunAsync02()
        {
            var activityExecutionResultMock = new Mock<IActivityExecutionResult>();
            var activity = CreateActivity(true, activityExecutionResultMock.Object);
            var workflow = CreateWorkflow(activity);
            var executionContext = await workflowHost.RunWorkflowAsync(workflow);

            activityExecutionResultMock
                .Verify(x => x.ExecuteAsync(It.IsAny<ActivityExecutionContext>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        private IActivity CreateActivity(bool canExecute = true, IActivityExecutionResult? activityExecutionResult = null)
        {
            var activityMock = new Mock<IActivity>();
            var activityId = fixture.Create<string>();

            activityMock.Setup(x => x.Id).Returns(activityId);
            
            activityMock
                .Setup(x => x.CanExecuteAsync(It.IsAny<ActivityExecutionContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(canExecute);

            if (activityExecutionResult != null)
                activityMock
                    .Setup(x => x.ExecuteAsync(It.IsAny<ActivityExecutionContext>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(activityExecutionResult);

            return activityMock.Object;
        }

        private Workflow CreateWorkflow(IActivity activity)
        {
            var workflow = new Workflow();
            workflow.Activities.Add(activity);
            return workflow;
        }
    }
}