using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Extensions;
using Flowsharp.Models;
using Flowsharp.Web.Abstractions.Services;
using Flowsharp.Web.ViewComponents.ViewModels;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.DisplayManagement;

namespace Flowsharp.Web.ViewComponents.ViewComponents
{
    public class ActivityPicker : ViewComponent
    {
        private readonly IActivityLibrary activityLibrary;
        private readonly IActivityDisplayManager activityDisplayManager;

        public ActivityPicker(IActivityLibrary activityLibrary, IActivityDisplayManager activityDisplayManager)
        {
            this.activityLibrary = activityLibrary;
            this.activityDisplayManager = activityDisplayManager;
        }

        public async Task<IViewComponentResult> InvokeAsync(CancellationToken cancellationToken)
        {
            var activityDescriptors = await activityLibrary.GetActivitiesAsync(cancellationToken).ToListAsync();
            var categories = await activityLibrary.GetCategoriesAsync(cancellationToken);
            var cardShapes = await Task.WhenAll(activityDescriptors.Select(CreateCardShapeAsync));
            var viewModel = new ActivityPickerViewModel(categories, activityDescriptors, cardShapes);

            return View(viewModel);
        }

        private Task<IShape> CreateCardShapeAsync(ActivityDescriptor descriptor)
        {
            var activity = descriptor.InstantiateActivity();
            return activityDisplayManager.BuildDisplayAsync(activity, null, "Card");
        }
    }
}