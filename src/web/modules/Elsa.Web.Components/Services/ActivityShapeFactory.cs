using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Web.Components.Models;
using Elsa.Web.Services;
using OrchardCore.DisplayManagement;

namespace Elsa.Web.Components.Services
{
    public class ActivityShapeFactory : IActivityShapeFactory
    {
        private readonly IActivityDisplayManager displayManager;

        public ActivityShapeFactory(IActivityDisplayManager displayManager)
        {
            this.displayManager = displayManager;
        }
        
        public async Task<IShape> BuildDesignShapeAsync(IActivity activity, CancellationToken cancellationToken)
        {
            var descriptor = activity.Descriptor;
            dynamic shape = await displayManager.BuildDisplayAsync(activity, null, "Design");
            var customFields = activity.Metadata.CustomFields;
            var designerMetadata = customFields.GetValue("Designer", StringComparison.OrdinalIgnoreCase)?.ToObject<ActivityDesignerMetadata>() ?? new ActivityDesignerMetadata();
            
            shape.Metadata.Type = $"Activity_Design";
            shape.Endpoints = descriptor.GetEndpoints().ToList();
            shape.ActivityDescriptor = descriptor;
            shape.Activity = activity;
            shape.Designer = designerMetadata;

            return shape;
        }

        public async Task<IShape> BuildCardShapeAsync(IActivity activity, CancellationToken cancellationToken)
        {
            var descriptor = activity.Descriptor;
            dynamic shape = await displayManager.BuildDisplayAsync(activity, null, "Card");
            
            shape.Metadata.Type = $"Activity_Card";
            shape.ActivityDescriptor = descriptor;

            return shape;
        }
    }
}