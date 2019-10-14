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
using Elsa.Serialization;
using Elsa.Serialization.Formatters;
using Elsa.Services;
using Jint.Native.Json;
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
        private readonly IWorkflowSerializer serializer;
        private readonly IWorkflowFactory workflowFactory;
        private readonly INotifier notifier;

        public WorkflowInstanceController(
            IWorkflowInstanceStore workflowInstanceStore,
            IWorkflowDefinitionStore workflowDefinitionStore,
            IOptions<ElsaDashboardOptions> options,
            IWorkflowSerializer serializer,
            IWorkflowFactory workflowFactory,
            INotifier notifier)
        {
            this.workflowInstanceStore = workflowInstanceStore;
            this.workflowDefinitionStore = workflowDefinitionStore;
            this.options = options;
            this.serializer = serializer;
            this.workflowFactory = workflowFactory;
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

            var workflow = workflowFactory.CreateWorkflow(definition, Variables.Empty, instance);

            var model = new WorkflowInstanceDetailsModel
            {
                ReturnUrl = returnUrl,
                WorkflowDefinition = definition,
                Workflow = workflow,
                ActivityDefinitions = options.Value.ActivityDefinitions.ToArray()
            };

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
    }
}