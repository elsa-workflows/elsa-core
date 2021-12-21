using System.Threading.Tasks;
using Elsa.Activities.Temporal;
using Elsa.Activities.Temporal.Common.Services;
using Elsa.Models;
using Elsa.Services;
using Elsa.Testing.Shared.Unit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NodaTime;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.Core.IntegrationTests.Workflows
{
    public class TimerWorkflowTests : WorkflowsUnitTestBase
    {
        private static readonly Mock<IWorkflowDefinitionScheduler> WorkflowDefinitionScheduler = new();
        private static readonly Mock<IWorkflowInstanceScheduler> WorkflowInstanceScheduler = new ();
        private static readonly Mock<IClock> Clock = new();

        public TimerWorkflowTests(ITestOutputHelper testOutputHelper)
            : base(
                testOutputHelper,
                services =>
                {
                    services.AddSingleton(WorkflowDefinitionScheduler.Object);
                    services.AddSingleton(WorkflowInstanceScheduler.Object);
                    services.Replace(new ServiceDescriptor(typeof(IClock), Clock.Object));
                },
                options =>
                {
                    options.AddCommonTemporalActivities();
                    options.AddWorkflow<TimerWorkflow>();
                }) { }

        [Fact(DisplayName = "Runs timer workflow.")]
        public async Task Test01()
        {
            var now = new Instant();
            
            Clock
                .Setup(x => x.GetCurrentInstant())
                .Returns(now);

            var runWorkflowResult = await WorkflowBuilderAndStarter.BuildAndStartWorkflowAsync<TimerWorkflow>();
            var workflowInstance = runWorkflowResult.WorkflowInstance!;

            Assert.Equal(WorkflowStatus.Suspended, workflowInstance.WorkflowStatus);

            var timerId = workflowInstance.LastExecutedActivityId!;

            WorkflowInstanceScheduler
                .Verify(x => x
                    .ScheduleAsync(
                        workflowInstance.Id,
                        timerId,
                        now.Plus(Duration.FromSeconds(1)),
                        null,
                        default));

            var workflowResumer = ServiceProvider.GetRequiredService<IResumesWorkflow>();
            await workflowResumer.ResumeWorkflowAsync(workflowInstance, timerId);
            
            Assert.Equal(WorkflowStatus.Finished, workflowInstance.WorkflowStatus);
            
            WorkflowInstanceScheduler
                .Verify(x => x
                    .UnscheduleAsync(
                        workflowInstance.Id, 
                        timerId!,
                        default));
        }
    }
}