using Microsoft.Extensions.Localization;
using OrchardCore.DisplayManagement.Views;

namespace Elsa.Web.ViewModels
{
    public class ActivityShapeModel<TActivity> : ShapeViewModel where TActivity : IActivity
    {
        public ActivityShapeModel(TActivity activity)
        {
            Activity = activity;
        }

        public TActivity Activity { get; }
        public LocalizedString DisplayText => string.IsNullOrWhiteSpace(Activity.Metadata.Title) 
                ? Activity.Descriptor.DisplayText 
                : new LocalizedString(Activity.Metadata.Title, Activity.Metadata.Title);
    }
}
