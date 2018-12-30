using Flowsharp.Models;
using OrchardCore.DisplayManagement.Views;

namespace Flowsharp.Web.ViewComponents.ViewModels
{
    public class ActivityCardViewModel : ShapeViewModel<ActivityDescriptor>
    {
        public ActivityCardViewModel(ActivityDescriptor value) : base(value)
        {
        }
    }
}