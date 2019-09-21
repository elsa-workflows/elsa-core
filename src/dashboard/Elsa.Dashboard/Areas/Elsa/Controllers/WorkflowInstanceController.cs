using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Dashboard.Areas.Elsa.ViewModels;
using Elsa.Dashboard.Extensions;
using Elsa.Dashboard.Models;
using Elsa.Dashboard.Services;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Serialization;
using Elsa.Serialization.Formatters;
using Jint.Native.Json;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Dashboard.Areas.Elsa.Controllers
{
    [Area("Elsa")]
    [Route("[area]/workflow-instance")]
    public class WorkflowInstanceController : Controller
    {
        private readonly IWorkflowInstanceStore workflowInstanceStore;
        private readonly IWorkflowDefinitionStore workflowDefinitionStore;
        private readonly IWorkflowSerializer serializer;
        private readonly INotifier notifier;

        public WorkflowInstanceController(
            IWorkflowInstanceStore workflowInstanceStore,
            IWorkflowDefinitionStore workflowDefinitionStore,
            IWorkflowSerializer serializer,
            INotifier notifier)
        {
            this.workflowInstanceStore = workflowInstanceStore;
            this.workflowDefinitionStore = workflowDefinitionStore;
            this.serializer = serializer;
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

            var json = serializer.Serialize(definition, JsonTokenFormatter.FormatName);

            var model = new WorkflowInstanceDetailsModel
            {
                ReturnUrl = returnUrl,
                Json = json,
                WorkflowDefinition = definition,
                WorkflowInstance = instance
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