using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using Elsa.ActivityResults;
using Elsa.Expressions;
using Elsa.Models;
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
using YesSql;
using YesSql.Provider.Sqlite;

namespace Elsa.Core.UnitTests
{
    public class WorkflowHostTests : IDisposable
    {
        private readonly IFixture _fixture;
        private readonly WorkflowHost _workflowHost;
        private TemporaryFolder _tempFolder;
        private ISession _session;

        public WorkflowHostTests()
        {
            _fixture = new Fixture().Customize(new NodaTimeCustomization());
            _session = CreateSession();
            
            var workflowActivatorMock = new Mock<IWorkflowActivator>();
            var workflowRegistryMock = new Mock<IWorkflowRegistry>();
            var workflowInstanceManager = new WorkflowInstanceManager(_session);
            var workflowExpressionEvaluatorMock = new Mock<IExpressionEvaluator>();
            var mediatorMock = new Mock<IMediator>();
            var now = _fixture.Create<Instant>();
            var clock = new FakeClock(now);
            var logger = new NullLogger<WorkflowHost>();
            var serviceProvider = new ServiceCollection().BuildServiceProvider();

            workflowActivatorMock
                .Setup(x => x.ActivateAsync(It.IsAny<Workflow>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Workflow workflow, string? correlationId, CancellationToken cancellationToken) => new WorkflowInstance());
            
            _workflowHost = new WorkflowHost(
                workflowInstanceManager,
                workflowRegistryMock.Object,
                workflowActivatorMock.Object,
                workflowExpressionEvaluatorMock.Object,
                clock,
                mediatorMock.Object,
                serviceProvider,
                logger);
        }
        
        public void Dispose() => _session.Dispose();

        private ISession CreateSession()
        {
            _tempFolder = new TemporaryFolder();
            var connectionString = $@"Data Source={_tempFolder.Folder}elsa.db;Cache=Shared";
            var config = new Configuration().UseSqLite(connectionString).UseDefaultIdGenerator();
            var store = StoreFactory.CreateAndInitializeAsync(config).GetAwaiter().GetResult();
            return store.CreateSession();
        }

        [Fact(DisplayName = "Can run simple workflow to completed state.")]
        public async Task RunAsync01()
        {
            var activityExecutionResultMock = new Mock<IActivityExecutionResult>();
            var activity = CreateActivity(activityExecutionResult: activityExecutionResultMock.Object);
            var workflow = CreateWorkflow(activity);
            var executionContext = await _workflowHost.RunWorkflowAsync(workflow);

            Assert.Equal(WorkflowStatus.Completed, executionContext.UpdateWorkflowInstance().Status);
        }

        [Fact(DisplayName = "Invokes returned activity execution result.")]
        public async Task RunAsync02()
        {
            var activityExecutionResultMock = new Mock<IActivityExecutionResult>();
            var activity = CreateActivity(true, activityExecutionResultMock.Object);
            var workflow = CreateWorkflow(activity);
            var executionContext = await _workflowHost.RunWorkflowAsync(workflow);

            activityExecutionResultMock
                .Verify(x => x.ExecuteAsync(It.IsAny<ActivityExecutionContext>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        private IActivity CreateActivity(bool canExecute = true, IActivityExecutionResult? activityExecutionResult = null)
        {
            var activityMock = new Mock<IActivity>();
            var activityId = _fixture.Create<string>();

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