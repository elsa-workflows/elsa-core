using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.ModelBinding;

namespace Flowsharp.Web.ViewComponents.Controllers
{
    [Route("[controller]")]
    [IgnoreAntiforgeryToken]
    public class ActivityController : Controller, IUpdateModel
    {
        private readonly IDisplayManager<Flowsharp.Models.ActivityDescriptor> displayManager;
        private readonly IActivityLibrary activityLibrary;

        public ActivityController(IDisplayManager<Flowsharp.Models.ActivityDescriptor> displayManager, IActivityLibrary activityLibrary)
        {
            this.displayManager = displayManager;
            this.activityLibrary = activityLibrary;
        }
        
        [HttpPost("display/{activityName}")]
        public async Task<IActionResult> Display(string activityName, [FromBody] JToken json, CancellationToken cancellationToken)
        {
            var activity = await activityLibrary.GetActivityByNameAsync(activityName, cancellationToken);
            var editorShape = await displayManager.BuildEditorAsync(activity, this, true);
            return View(editorShape);
        }
    }
}