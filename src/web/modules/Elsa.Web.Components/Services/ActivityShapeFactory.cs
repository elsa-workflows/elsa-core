using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Web.Components.Models;
using Elsa.Web.Services;
using OrchardCore.DisplayManagement;

namespace Elsa.Web.Components.Services
{
    public class ActivityShapeFactory : IActivityShapeFactory
    {
        private readonly IActivityDisplayManager displayManager;
        private readonly IShapeFactory shapeFactory;
        private readonly IActivityDesignerStore activityDesignerStore;

        public ActivityShapeFactory(IActivityDisplayManager displayManager, IShapeFactory shapeFactory, IActivityDesignerStore activityDesignerStore)
        {
            this.displayManager = displayManager;
            this.shapeFactory = shapeFactory;
            this.activityDesignerStore = activityDesignerStore;
        }
        
        public async Task<IShape> BuildDesignShapeAsync(IActivity activity, CancellationToken cancellationToken)
        {
            var descriptor = activity.Descriptor;
            var designer = await activityDesignerStore.GetByTypeNameAsync(activity.TypeName, cancellationToken);
            dynamic shape = await displayManager.BuildDisplayAsync(activity, null, "Design");
            var customFields = activity.Metadata.CustomFields;
            var designerMetadata = customFields.GetValue("Designer", StringComparison.OrdinalIgnoreCase)?.ToObject<ActivityDesignerMetadata>() ?? new ActivityDesignerMetadata();
            
            shape.Metadata.Type = $"Activity_Design";
            shape.Endpoints = designer.EndPoints(activity).ToList();
            shape.ActivityDescriptor = descriptor;
            shape.ActivityDesigner = designer;
            shape.Activity = activity;
            shape.Designer = designerMetadata;
            shape.IsBlocking = false;
            shape.HasExecuted = false;
            shape.HasFaulted = false;
            shape.WorkflowIsDefinition = true;

            return shape;
        }

        public async Task<IShape> BuildCardShapeAsync(IActivity activity, CancellationToken cancellationToken)
        {
            dynamic shape = await shapeFactory.New.Activity_Card2();
            var designer = await activityDesignerStore.GetByTypeNameAsync(activity.TypeName, cancellationToken);
            shape.ActivityDescriptor = activity.Descriptor;
            shape.ActivityDesigner = designer;
            shape.Activity = activity;
            return shape;
        }
    }
}