using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;
using Flowsharp.Web.ViewComponents.ViewModels;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.Views;

namespace Flowsharp.Web.ViewComponents.ViewComponents
{
    public class ActivityLibrary : ViewComponent
    {
        private readonly IActivityLibrary activityLibrary;
        private readonly IShapeFactory shapeFactory;

        public ActivityLibrary(IActivityLibrary activityLibrary,
            IShapeFactory shapeFactory)
        {
            this.activityLibrary = activityLibrary;
            this.shapeFactory = shapeFactory;
        }
        
        public async Task<IViewComponentResult> InvokeAsync(CancellationToken cancellationToken)
        {
            var descriptors = await activityLibrary.GetActivitiesAsync(cancellationToken);
            var activityShapes = await BuildActivityShapesAsync(descriptors);
            var viewModel = new ActivityLibraryViewModel(activityShapes);
            
            return View(viewModel);
        }

        private async Task<ICollection<IShape>> BuildActivityShapesAsync(IEnumerable<ActivityDescriptor> descriptors)
        {
            return await Task.WhenAll(descriptors.Select(BuildActivityShapeAsync));
        }
        
        private async Task<IShape> BuildActivityShapeAsync(ActivityDescriptor descriptor)
        {
            var activityShape = await shapeFactory.CreateAsync("Activity_Card", () => Task.FromResult<IShape>(new ActivityDescriptorViewModel(descriptor)));
            activityShape.Metadata.Alternates.Add($"Activity_Card__{descriptor.Name}");
            activityShape.Metadata.Type = "Activity_Card";
            return activityShape;
        }
    }
}