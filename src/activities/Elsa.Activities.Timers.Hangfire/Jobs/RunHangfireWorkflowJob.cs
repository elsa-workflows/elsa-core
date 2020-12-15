using System;
using System.Threading.Tasks;

using Elsa.Activities.Timers.Hangfire.Models;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services;

using Hangfire;

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

            if (data.WorkflowInstanceId == null)
            {
                if (workflowBlueprint.IsSingleton && await _workflowInstanceManager.GetWorkflowIsAlreadyExecutingAsync(data.TenantId, data.WorkflowDefinitionId))
                    return;

                await _workflowRunner.RunWorkflowAsync(workflowBlueprint, data.ActivityId);
            }
            else
            {
                var workflowInstance = await _workflowInstanceManager.GetByIdAsync(data.WorkflowInstanceId);

                if (workflowInstance == null)
                {
                    var logger = _serviceProvider.GetRequiredService<ILogger<RunHangfireWorkflowJob>>();
                    logger.LogWarning("Could not run Workflow instance with ID {WorkflowInstanceId} because it appears not yet to be persisted in the database. Rescheduling.", data.WorkflowInstanceId);
                   
                    if(data.RecurringJob)
                    {
                        return;
                    }

                    var _backgroundJobClient = _serviceProvider.GetRequiredService<IBackgroundJobClient>();
                    _backgroundJobClient.ScheduleWorkflow(data);
                    return;
                }

                await _workflowRunner.RunWorkflowAsync(workflowBlueprint, workflowInstance!, data.ActivityId);
            }
        }
    }
}
