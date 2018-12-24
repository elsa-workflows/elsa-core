using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Extensions;
using Flowsharp.Models;
using Flowsharp.Persistence;
using Flowsharp.Web.Abstractions.Services;
using Flowsharp.Web.ViewComponents.Models;
using Flowsharp.Web.ViewComponents.ViewModels;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.DisplayManagement;

namespace Flowsharp.Web.ViewComponents.ViewComponents
{
    public class ActivityPicker : ViewComponent
    {
        private readonly IActivityLibrary activityLibrary;
        private readonly IShapeFactory shapeFactory;

        public ActivityPicker(
            IActivityLibrary activityLibrary,
            IShapeFactory shapeFactory)
        {
            this.activityLibrary = activityLibrary;
            this.shapeFactory = shapeFactory;
        }
        
        public async Task<IViewComponentResult> InvokeAsync(CancellationToken cancellationToken)
        {
            var activities = await activityLibrary.GetActivitiesAsync(cancellationToken);
            var activityShapes = await BuildActivityShapesAsync(activities, cancellationToken);
            var categories = await activityLibrary.GetCategoriesAsync(cancellationToken);
            var viewModel = new ActivityPickerViewModel(activityShapes, categories);
            
            return View(viewModel);
        }

        private async Task<ICollection<IShape>> BuildActivityShapesAsync(IEnumerable<ActivityDescriptor> activities, CancellationToken cancellationToken)
        {
            return await Task.WhenAll(activities.Select((x, i) => BuildActivityShapeAsync(x, cancellationToken)));
        }
        
        private async Task<IShape> BuildActivityShapeAsync(ActivityDescriptor activityDescriptor, CancellationToken cancellationToken)
        {
            return await shapeFactory.CreateAsync("Activity_Card", new ActivityCardViewModel(activityDescriptor));
        }
    }
}