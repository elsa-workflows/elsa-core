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
        private readonly IActivityShapeFactory activityShapeFactory;

        public ActivityPicker(IActivityLibrary activityLibrary, IActivityShapeFactory activityShapeFactory)
        {
            this.activityLibrary = activityLibrary;
            this.activityShapeFactory = activityShapeFactory;
        }

        public async Task<IViewComponentResult> InvokeAsync(CancellationToken cancellationToken)
        {
            var descriptors = await activityLibrary.ListBrowsableAsync(cancellationToken).ToListAsync();
            var categories = await activityLibrary.GetCategoriesAsync(cancellationToken);
            var cardShapes = await Task.WhenAll(descriptors.Select(x => CreateCardShapeAsync(x, cancellationToken)));
            var viewModel = new ActivityPickerViewModel(categories, descriptors, cardShapes);

            return View(viewModel);
        }

        private Task<IShape> CreateCardShapeAsync(ActivityDescriptor descriptor, CancellationToken cancellationToken)
        {
            var activity = descriptor.InstantiateActivity();
            return activityShapeFactory.BuildCardShapeAsync(activity, cancellationToken);
        }
    }
}