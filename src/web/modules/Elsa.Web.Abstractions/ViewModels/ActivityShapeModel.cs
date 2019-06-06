using Elsa.Models;
using Microsoft.Extensions.Localization;
using OrchardCore.DisplayManagement.Views;

namespace Elsa.Web.ViewModels
{
    public class ActivityShapeModel<TActivity> : ShapeViewModel where TActivity : IActivity
    {
        public ActivityShapeModel(TActivity activity, ActivityDesignerDescriptor designer)
        {
            Activity = activity;
            Designer = designer;
        }

        public TActivity Activity { get; }
        public ActivityDesignerDescriptor Designer { get; }

        public LocalizedString DisplayText => string.IsNullOrWhiteSpace(Activity.Metadata.Title) 
            ? Designer.DisplayName
            : new LocalizedString(Activity.Metadata.Title, Activity.Metadata.Title);
    }
}
