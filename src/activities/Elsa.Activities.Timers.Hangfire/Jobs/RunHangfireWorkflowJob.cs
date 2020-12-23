using System;
using System.Threading.Tasks;

using Elsa.Activities.Timers.Hangfire.Models;
using Elsa.Activities.Timers.Services;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services;

using Hangfire;
using Hangfire.States;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Activities.Timers.Hangfire.Jobs
{
    public class RunHangfireWorkflowJob
    {
        private readonly IWorkflowRunner _workflowRunner;
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IWorkflowInstanceStore _workflowInstanceManager;
        private readonly IServiceProvider _serviceProvider;

        public RunHangfireWorkflowJob(IWorkflowRunner workflowRunner, IWorkflowRegistry workflowRegistry, IWorkflowInstanceStore workflowInstanceStore, IServiceProvider serviceProvider)
        {
            _workflowRunner = workflowRunner;
            _workflowRegistry = workflowRegistry;
            _workflowInstanceManager = workflowInstanceStore;
            _serviceProvider = serviceProvider;
        }

        public async Task ExecuteAsync(RunHangfireWorkflowJobModel data)
        {
            var workflowBlueprint = (await _workflowRegistry.GetWorkflowAsync(data.WorkflowDefinitionId, data.TenantId, VersionOptions.Published))!;
            
            if(workflowBlueprint == null)
            {
                return;
            }

            WorkflowInstance? workflowInstance = null;

            if (data.WorkflowInstanceId == null)
            {
                if (workflowBlueprint.IsSingleton == false || await _workflowInstanceManager.GetWorkflowIsAlreadyExecutingAsync(data.TenantId, data.WorkflowDefinitionId) == false)
                {
                    await _workflowRunner.RunWorkflowAsync(workflowBlueprint, data.ActivityId);
                }
            }
            else
            {
                workflowInstance = await GetWorkflowInstanceAsync(data.WorkflowInstanceId);

                if (workflowInstance == null)
                {
                    var logger = _serviceProvider.GetRequiredService<ILogger<RunHangfireWorkflowJob>>();
                    logger.LogError("Could not run Workflow instance with ID {WorkflowInstanceId} because it is not in the database", data.WorkflowInstanceId);

                    return;
                }

                await _workflowRunner.RunWorkflowAsync(workflowBlueprint, workflowInstance!, data.ActivityId);
            }

            // If it is a RecurringJob and the instance is null, the timer activity is a start trigger.
            if (data.IsRecurringJob && (workflowInstance == null || workflowInstance.Status is not (WorkflowStatus.Finished or WorkflowStatus.Cancelled)))
            {
                var backgroundJobClient = _serviceProvider.GetRequiredService<IBackgroundJobClient>();
                var crontabParer = _serviceProvider.GetRequiredService<ICrontabParser>();

                backgroundJobClient.ScheduleWorkflow(data, crontabParer.GetNextOccurrence(data.CronExpression!).ToDateTimeOffset());
            }
        }

        private async Task<WorkflowInstance?> GetWorkflowInstanceAsync(string workflowInstanceId)
        {
            WorkflowInstance? workflowInstance = null;

            for (var i = 0; i < TimerConsts.MaxRetrayGetWorkflow && workflowInstance == null; i++)
            {
                workflowInstance = await _workflowInstanceManager.GetByIdAsync(workflowInstanceId);

                if (workflowInstance == null)
                {
                    System.Threading.Thread.Sleep(10000);
                }
            }

            return workflowInstance;
        }
    }
}
