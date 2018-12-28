using System;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Extensions;
using Flowsharp.Web.Abstractions.Services;
using Flowsharp.Web.ViewComponents.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OrchardCore.DisplayManagement.ModelBinding;

namespace Flowsharp.Web.ViewComponents.Controllers
{
    [Route("[controller]")]
    [IgnoreAntiforgeryToken]
    public class ActivityController : Controller, IUpdateModel
    {
        private readonly IActivityDisplayManager displayManager;
        private readonly IActivityLibrary activityLibrary;
        private readonly IActivityShapeFactory activityShapeFactory;

        public ActivityController(
            IActivityDisplayManager displayManager,
            IActivityLibrary activityLibrary,
            IActivityShapeFactory activityShapeFactory)
        {
            this.displayManager = displayManager;
            this.activityLibrary = activityLibrary;
            this.activityShapeFactory = activityShapeFactory;
        }

        [HttpPost("create/{activityName}")]
        public async Task<IActionResult> Create(string activityName, [FromBody] JToken json, CancellationToken cancellationToken)
        {
            var activityDescriptor = await activityLibrary.GetActivityByNameAsync(activityName, cancellationToken);
            var activity = activityDescriptor.InstantiateActivity(json);

            activity.Id = Guid.NewGuid().ToString();

            var editorShape = await displayManager.BuildEditorAsync(activity, this, false);
            var model = new ActivityEditorEditViewModel
            {
                ActivityJson = JsonConvert.SerializeObject(activity),
                ActivityEditorShape = editorShape,
            };
            return View("Edit", model);
        }

        [HttpPost("edit/{activityName}")]
        public async Task<IActionResult> Edit(string activityName, [FromBody] JToken json, CancellationToken cancellationToken)
        {
            var activityDescriptor = await activityLibrary.GetActivityByNameAsync(activityName, cancellationToken);
            var activity = activityDescriptor.InstantiateActivity(json);
            var editorShape = await displayManager.BuildEditorAsync(activity, this, false);
            var model = new ActivityEditorEditViewModel
            {
                ActivityJson = json.ToString(),
                ActivityEditorShape = editorShape,
            };
            return View(model);
        }

        [HttpPost("update/{activityName}")]
        public async Task<IActionResult> Update(string activityName, [FromForm] ActivityEditorUpdateModel model, CancellationToken cancellationToken)
        {
            var activityDescriptor = await activityLibrary.GetActivityByNameAsync(activityName, cancellationToken);
            var activity = (IActivity) JToken.Parse(model.ActivityJson ?? "{}").ToObject(activityDescriptor.ActivityType);
            var editorShape = await displayManager.UpdateEditorAsync(activity, this, false);

            if (!ModelState.IsValid)
                return View("Edit", new ActivityEditorEditViewModel
                {
                    ActivityJson = JsonConvert.SerializeObject(activity),
                    ActivityEditorShape = editorShape
                });

            dynamic designShape = await activityShapeFactory.BuildDesignShapeAsync(activity, cancellationToken);
            return View("Display", designShape);
        }
    }
}