using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Extensions;
using Flowsharp.Models;
using Flowsharp.Web.Abstractions.Services;
using Flowsharp.Web.ViewComponents.Models;
using Newtonsoft.Json.Linq;
using OrchardCore.DisplayManagement;

namespace Flowsharp.Web.ViewComponents.Services
{
    public class ActivityShapeFactory : IActivityShapeFactory
    {
        private readonly IActivityDisplayManager displayManager;
        private readonly IActivityLibrary activityLibrary;

        public ActivityShapeFactory(IActivityDisplayManager displayManager, IActivityLibrary activityLibrary)
        {
            this.displayManager = displayManager;
            this.activityLibrary = activityLibrary;
        }
        
        public async Task<IShape> BuildDesignShapeAsync(IActivity activity, CancellationToken cancellationToken)
        {
            dynamic shape = await displayManager.BuildDisplayAsync(activity, null, "Design");
            var customFields = activity.Metadata.CustomFields;
            var designerMetadata = customFields.GetValue("Designer", StringComparison.OrdinalIgnoreCase)?.ToObject<ActivityDesignerMetadata>() ?? new ActivityDesignerMetadata();
            var descriptor = await activityLibrary.GetActivityByNameAsync(activity.Name, CancellationToken.None);
            
            shape.Metadata.Type = $"Activity_Design";
            shape.Endpoints = descriptor.GetEndpoints().ToList();
            shape.ActivityDescriptor = descriptor;
            shape.Activity = activity;
            shape.Designer = designerMetadata;

            return shape;
        }
    }
}