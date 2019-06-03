using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Web.Services;
using Elsa.Web.Shapes;

namespace Elsa.Web.Components.Services
{
    public class ActivityShapeFactory : IActivityShapeFactory
    {
        private readonly IActivityStore activityStore;
        private readonly IActivityDesignerStore activityDesignerStore;

        public ActivityShapeFactory(
            IActivityStore activityStore,
            IActivityDesignerStore activityDesignerStore)
        {
            this.activityStore = activityStore;
            this.activityDesignerStore = activityDesignerStore;
        }

        public async Task<ActivityWrapper> BuildDesignShapeAsync(IActivity activity, CancellationToken cancellationToken)
        {
            var descriptor = await activityStore.GetByTypeNameAsync(activity.TypeName, cancellationToken);
            var designer = await activityDesignerStore.GetByTypeNameAsync(activity.TypeName, cancellationToken);
            var shape = new ActivityDesign(activity, descriptor, designer);
            var wrapper = new ActivityWrapper(activity, descriptor, designer, shape);
            
            shape.Metadata.Alternates.Add($"Activity_Design__{descriptor.ActivityTypeName}");
            
            return wrapper;
        }

        public async Task<ActivityCard> BuildCardShapeAsync(IActivity activity, CancellationToken cancellationToken)
        {
            var descriptor = await activityStore.GetByTypeNameAsync(activity.TypeName, cancellationToken);
            var designer = await activityDesignerStore.GetByTypeNameAsync(activity.TypeName, cancellationToken);
            return new ActivityCard(activity, descriptor, designer);
        }
    }
}