using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using Elsa.Expressions;
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
        private readonly ProcessRunner runner;

        public WorkflowRunnerTests()
        {
            var fixture = new Fixture().Customize(new NodaTimeCustomization());
            var activityInvokerMock = new Mock<IActivityInvoker>();
            var workflowFactoryMock = new Mock<IProcessFactory>();
            var workflowRegistryMock = new Mock<IProcessRegistry>();
            var workflowInstanceStoreMock = new Mock<IWorkflowInstanceStore>();
            var workflowExpressionEvaluatorMock = new Mock<IExpressionEvaluator>();
            var mediatorMock = new Mock<IMediator>();
            var now = fixture.Create<Instant>();
            var fakeClock = new FakeClock(now);
            var logger = new NullLogger<ProcessRunner>();
            var serviceProvider = new ServiceCollection().BuildServiceProvider();

            activityInvokerMock
                .Setup(x => x.ExecuteAsync(It.IsAny<ProcessExecutionContext>(), It.IsAny<IActivity>(), It.IsAny<Variable>(), It.IsAny<CancellationToken>()))
                .Returns(async (ProcessExecutionContext a, IActivity b, Variable c, CancellationToken d) => await b.ExecuteAsync(new ActivityExecutionContext(a, c), d));

            runner = new ProcessRunner(
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
            var activity = CreateActivity();
            var workflow = CreateWorkflow(activity);
            var executionContext = await runner.RunAsync(workflow);

            Assert.Equal(ProcessStatus.Completed, executionContext.ProcessInstance.Status);
        }

        [Fact(DisplayName = "Invokes returned activity execution result.")]
        public async Task RunAsync02()
        {
            var activityExecutionResultMock = new Mock<IActivityExecutionResult>();
            var activity = CreateActivity(true, activityExecutionResultMock.Object);
            var workflow = CreateWorkflow(activity);
            var executionContext = await runner.RunAsync(workflow);

            activityExecutionResultMock
                .Verify(x => x.ExecuteAsync(runner, executionContext, It.IsAny<CancellationToken>()), Times.Once);
        }

        private IActivity CreateActivity(bool canExecute = true, IActivityExecutionResult? activityExecutionResult = null)
        {
            var activityMock = new Mock<IActivity>();

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
            var blueprint = new WorkflowBlueprint();
            blueprint.Activities.Add(activity);
            return new Workflow(blueprint);
        }
    }
}