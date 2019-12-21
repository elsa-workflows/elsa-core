using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using Elsa.Models;
using Elsa.Persistence;
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
    public class WorkflowRunnerTests
    {
        private readonly WorkflowRunner runner;

        public WorkflowRunnerTests()
        {
            var fixture = new Fixture().Customize(new NodaTimeCustomization());
            var activityInvokerMock = new Mock<IActivityInvoker>();
            var workflowFactoryMock = new Mock<IWorkflowFactory>();
            var workflowRegistryMock = new Mock<IWorkflowRegistry>();
            var workflowInstanceStoreMock = new Mock<IWorkflowInstanceStore>();
            var workflowExpressionEvaluatorMock = new Mock<IWorkflowExpressionEvaluator>();
            var mediatorMock = new Mock<IMediator>();
            var now = fixture.Create<Instant>();
            var fakeClock = new FakeClock(now);
            var logger = new NullLogger<WorkflowRunner>();
            var serviceProvider = new ServiceCollection().BuildServiceProvider();

            activityInvokerMock
                .Setup(x => x.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<IActivity>(), It.IsAny<CancellationToken>()))
                .Returns(async (WorkflowExecutionContext a, IActivity b, CancellationToken c) => await b.ExecuteAsync(a, c));

            runner = new WorkflowRunner(
                activityInvokerMock.Object,
                workflowFactoryMock.Object,
                workflowRegistryMock.Object,
                workflowInstanceStoreMock.Object,
                workflowExpressionEvaluatorMock.Object,
                fakeClock,
                mediatorMock.Object,
                serviceProvider,
                logger);
        }

        [Fact(DisplayName = "Can run simple workflow to completed state.")]
        public async Task RunAsync01()
        {
            var workflow = new Workflow();
            var activity = CreateActivity();

            workflow.Activities.Add(activity);

            var executionContext = await runner.RunAsync(workflow);

            Assert.Equal(WorkflowStatus.Completed, executionContext.Workflow.Status);
        }

        [Fact(DisplayName = "Invokes returned activity execution result.")]
        public async Task RunAsync02()
        {
            var workflow = new Workflow();
            var activityExecutionResultMock = new Mock<IActivityExecutionResult>();
            var activity = CreateActivity(true, activityExecutionResultMock.Object);

            workflow.Activities.Add(activity);
            var executionContext = await runner.RunAsync(workflow);

            activityExecutionResultMock
                .Verify(x => x.ExecuteAsync(runner, executionContext, It.IsAny<CancellationToken>()), Times.Once);
        }

        private IActivity CreateActivity(bool canExecute = true, IActivityExecutionResult? activityExecutionResult = null)
        {
            var activityMock = new Mock<IActivity>();

            activityMock
                .Setup(x => x.CanExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(canExecute);

            if (activityExecutionResult != null)
                activityMock
                    .Setup(x => x.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(activityExecutionResult);

            return activityMock.Object;
        }
    }
}