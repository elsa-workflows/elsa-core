using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Web.Components.ViewModels;
using Elsa.Web.Services;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.DisplayManagement;

namespace Elsa.Web.Components.ViewComponents
{
    public class ActivityPicker : ViewComponent
    {
        private readonly IActivityStore activityStore;
        private readonly IActivityDesignerStore designerStore;
        private readonly IActivityShapeFactory activityShapeFactory;

        public ActivityPicker(IActivityStore activityStore, IActivityDesignerStore designerStore, IActivityShapeFactory activityShapeFactory)
        {
            this.activityStore = activityStore;
            this.designerStore = designerStore;
            this.activityShapeFactory = activityShapeFactory;
        }

        public async Task<IViewComponentResult> InvokeAsync(CancellationToken cancellationToken)
        {
            var descriptors = await designerStore.ListBrowsableAsync(cancellationToken).ToListAsync();
            var categories = await designerStore.GetCategoriesAsync(cancellationToken);
            var cardShapes = await Task.WhenAll(descriptors.Select(x => CreateCardShapeAsync(x, cancellationToken)));
            var viewModel = new ActivityPickerViewModel(categories, descriptors, cardShapes);

            return View(viewModel);
        }

        private async Task<IShape> CreateCardShapeAsync(ActivityDesignerDescriptor designer, CancellationToken cancellationToken)
        {
            var descriptor = await activityStore.GetByTypeNameAsync(designer.ActivityTypeName, cancellationToken); 
            var activity =  descriptor.InstantiateActivity();
            return await activityShapeFactory.BuildCardShapeAsync(activity, cancellationToken);
        }
    }
}