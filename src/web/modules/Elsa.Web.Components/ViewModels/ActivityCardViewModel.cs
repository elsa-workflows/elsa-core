using Elsa.Models;
using OrchardCore.DisplayManagement.Views;

namespace Elsa.Web.Components.ViewModels
{
    public class ActivityCardViewModel : ShapeViewModel<ActivityDescriptor>
    {
        public ActivityCardViewModel(ActivityDescriptor value) : base(value)
        {
        }
    }
}