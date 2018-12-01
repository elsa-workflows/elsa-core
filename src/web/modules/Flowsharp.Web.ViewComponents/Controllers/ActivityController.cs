using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Extensions;
using Flowsharp.Models;
using Flowsharp.Web.Abstractions.Services;
using Flowsharp.Web.ViewComponents.Models;
using Flowsharp.Web.ViewComponents.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OrchardCore.DisplayManagement;
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

        [HttpPost("edit/{activityName}")]
        public async Task<IActionResult> Edit(string activityName, [FromBody] string json, CancellationToken cancellationToken)
        {
            var activityDescriptor = await activityLibrary.GetActivityByNameAsync(activityName, cancellationToken);
            var activityModel = activityDescriptor.InstantiateActivity(json);
            var editorShape = await displayManager.BuildEditorAsync(activityModel, this, false);
            var model = new ActivityEditorViewModel
            {
                ActivityJson = json,
                ActivityEditorShape = editorShape,
            };
            return View(model);
        }

        [HttpPost("update/{activityName}")]
        public async Task<IActionResult> Update(string activityName, [FromForm] ActivityEditorUpdateModel model, CancellationToken cancellationToken)
        {
            var activityDescriptor = await activityLibrary.GetActivityByNameAsync(activityName, cancellationToken);
            var activityModel = (IActivity) JToken.Parse(model.ActivityJson ?? "{}").ToObject(activityDescriptor.ActivityType);
            var editorShape = await displayManager.UpdateEditorAsync(activityModel, this, false);

            if (!ModelState.IsValid)
                return View("Edit", new ActivityEditorViewModel
                {
                    ActivityJson = JsonConvert.SerializeObject(activityModel),
                    ActivityEditorShape = editorShape
                });

            dynamic designShape = await activityShapeFactory.BuildDesignShapeAsync(activityModel, cancellationToken);
            return View("Display", designShape);
        }
    }
}