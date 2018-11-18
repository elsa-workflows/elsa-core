using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;
using Flowsharp.Web.Abstractions.Services;
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

        public ActivityController(IActivityDisplayManager displayManager, IActivityLibrary activityLibrary)
        {
            this.displayManager = displayManager;
            this.activityLibrary = activityLibrary;
        }
        
        [HttpPost("display/{activityName}")]
        public async Task<IActionResult> Display(string activityName, [FromBody] string json, CancellationToken cancellationToken)
        {
            var activityDescriptor = await activityLibrary.GetActivityByNameAsync(activityName, cancellationToken);
            var activityModel = (IActivity)JToken.Parse(json ?? "{}").ToObject(activityDescriptor.ActivityType);
            var editorShape = await displayManager.BuildEditorAsync(activityModel, this, true);
            return View(editorShape);
        }
    }
}