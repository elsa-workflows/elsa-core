using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Dashboard.Areas.Elsa.ViewModels;
using Elsa.Dashboard.Extensions;
using Elsa.Dashboard.Models;
using Elsa.Dashboard.Options;
using Elsa.Dashboard.Services;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.WorkflowDesigner.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Elsa.Dashboard.Areas.Elsa.Controllers
{
    [Area("Elsa")]
    [Route("[area]/workflow-instance")]
    public class WorkflowInstanceController : Controller
    {
        private readonly IWorkflowInstanceStore workflowInstanceStore;
        private readonly IWorkflowDefinitionStore workflowDefinitionStore;
        private readonly IOptions<ElsaDashboardOptions> options;
        private readonly INotifier notifier;

        public WorkflowInstanceController(
            IWorkflowInstanceStore workflowInstanceStore,
            IWorkflowDefinitionStore workflowDefinitionStore,
            IOptions<ElsaDashboardOptions> options,
            INotifier notifier)
        {
            this.workflowInstanceStore = workflowInstanceStore;
            this.workflowDefinitionStore = workflowDefinitionStore;
            this.options = options;
            this.notifier = notifier;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string definitionId,
            WorkflowStatus status,
            CancellationToken cancellationToken)
        {
            var definition = await workflowDefinitionStore.GetByIdAsync(
                definitionId,
                VersionOptions.Latest,
                cancellationToken
            );

            if (definition == null)
                return NotFound();

            var instances = await workflowInstanceStore
                .ListByStatusAsync(definitionId, status, cancellationToken)
                .ToListAsync();

            var model = new WorkflowInstanceListViewModel
            {
                WorkflowDefinition = definition,
                ReturnUrl = Url.Action("Index", new { definitionId, status }),
                WorkflowInstances = instances.Select(
                        x => new WorkflowInstanceListItemModel
                        {
                            WorkflowInstance = x
                        }
                    )
                    .ToList()
            };

            return View(model);
        }

        [HttpGet("details/{id}")]
        public async Task<IActionResult> Details(string id, string returnUrl, CancellationToken cancellationToken)
        {
            var instance = await workflowInstanceStore.GetByIdAsync(id, cancellationToken);

            if (instance == null)
                return NotFound();

            var definition = await workflowDefinitionStore.GetByIdAsync(
                instance.DefinitionId,
                VersionOptions.SpecificVersion(instance.Version),
                cancellationToken
            );

            var workflow = new WorkflowModel
            {
                Activities = definition.Activities.Select(x => CreateActivityModel(x, instance)).ToList(),
                Connections = definition.Connections.Select(x => new ConnectionModel(x)).ToList()
            };

            var model = new WorkflowInstanceDetailsModel(
                instance,
                definition,
                workflow,
                options.Value.ActivityDefinitions,
                returnUrl);

            return View(model);
        }

        [HttpPost("delete/{id}")]
        public async Task<IActionResult> Delete(string id, string returnUrl, CancellationToken cancellationToken)
        {
            var instance = await workflowInstanceStore.GetByIdAsync(id, cancellationToken);

            if (instance == null)
                return NotFound();

            await workflowInstanceStore.DeleteAsync(id, cancellationToken);
            notifier.Notify("Workflow instance successfully deleted.", NotificationType.Success);

            if (Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "WorkflowDefinition");
        }

        private ActivityModel CreateActivityModel(
            ActivityDefinition activityDefinition,
            WorkflowInstance workflowInstance)
        {
            var isBlocking = workflowInstance.BlockingActivities.Any(x => x.ActivityId == activityDefinition.Id);
            var logEntry = workflowInstance.ExecutionLog.OrderByDescending(x => x.Timestamp)
                .FirstOrDefault(x => x.ActivityId == activityDefinition.Id);
            var isExecuted = logEntry != null;
            var isFaulted = logEntry?.Faulted ?? false;
            var message = default(ActivityMessageModel);

            if (isFaulted)
                message = new ActivityMessageModel("Faulted", logEntry.Message);
            else if (isBlocking)
                message = new ActivityMessageModel(
                    "Blocking",
                    "This activity is blocking workflow execution until the appropriate event is triggered.");
            else if (isExecuted)
                message = new ActivityMessageModel("Executed", logEntry.Message);

            return new ActivityModel(activityDefinition, isBlocking, isExecuted, isFaulted, message);
        }
    }
}