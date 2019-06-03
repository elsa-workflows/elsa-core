using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Serialization.Tokenizers;
using Elsa.Web.Components.ViewModels;
using Elsa.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OrchardCore.DisplayManagement.ModelBinding;

namespace Elsa.Web.Components.Controllers
{
    [Route("[controller]")]
    [IgnoreAntiforgeryToken]
    public class ActivityController : Controller, IUpdateModel
    {
        private readonly IActivityDisplayManager displayManager;
        private readonly IActivityStore activityStore;
        private readonly IWorkflowTokenizer workflowTokenizer;
        private readonly IActivityShapeFactory activityShapeFactory;

        public ActivityController(
            IActivityDisplayManager displayManager,
            IActivityStore activityStore,
            IWorkflowTokenizer workflowTokenizer,
            IActivityShapeFactory activityShapeFactory)
        {
            this.displayManager = displayManager;
            this.activityStore = activityStore;
            this.workflowTokenizer = workflowTokenizer;
            this.activityShapeFactory = activityShapeFactory;
        }

        [HttpPost("create/{activityName}")]
        public async Task<IActionResult> Create(string activityName, [FromBody] JToken json, CancellationToken cancellationToken)
        {
            var activityDescriptor = await activityStore.GetByTypeNameAsync(activityName, cancellationToken);
            var activity = activityDescriptor.InstantiateActivity(json);

            activity.Id = Guid.NewGuid().ToString("N");

            var editorShape2 = await activityShapeFactory.BuildEditShapeAsync(activity, cancellationToken);
            var editorShape = await displayManager.BuildEditorAsync(activity, this, false);
            var activityToken = await workflowTokenizer.TokenizeActivityAsync(activity, cancellationToken);

            var model = new ActivityEditorEditViewModel
            {
                ActivityJson = activityToken.ToString(),
                ActivityEditorShape = editorShape2,
            };
            
            return View("Edit", model);
        }

        [HttpPost("edit/{activityTypeName}")]
        public async Task<IActionResult> Edit(string activityTypeName, [FromBody] JToken json, CancellationToken cancellationToken)
        {
            var activityDescriptor = await activityStore.GetByTypeNameAsync(activityTypeName, cancellationToken);
            var activity = activityDescriptor.InstantiateActivity(json);
            var editorShape = await activityShapeFactory.BuildEditShapeAsync(activity, cancellationToken);
            //var editorShape = await displayManager.BuildEditorAsync(activity, this, false);
            var model = new ActivityEditorEditViewModel
            {
                ActivityJson = json.ToString(),
                ActivityEditorShape = editorShape,
            };
            return View(model);
        }

        [HttpPost("update/{activityTypeName}")]
        public async Task<IActionResult> Update([FromForm] ActivityEditorUpdateModel model, CancellationToken cancellationToken)
        {
            var activity = await workflowTokenizer.DetokenizeActivityAsync(JToken.Parse(model.ActivityJson ?? "{}"), cancellationToken);
            //var editorShape = await displayManager.UpdateEditorAsync(activity, this, false);
            await TryUpdateModelAsync(activity, activity.GetType(), activity.TypeName);

            if (!ModelState.IsValid)
                return View("Edit", new ActivityEditorEditViewModel
                {
                    ActivityJson = JsonConvert.SerializeObject(activity),
                    ActivityEditorShape = await activityShapeFactory.BuildEditShapeAsync(activity, cancellationToken)
                });

            dynamic designShape = await activityShapeFactory.BuildDesignShapeAsync(activity, cancellationToken);
            return View("Display", designShape);
        }
    }
}