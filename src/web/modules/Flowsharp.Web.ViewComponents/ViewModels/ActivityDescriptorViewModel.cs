using Flowsharp.Models;
using OrchardCore.DisplayManagement.Views;

namespace Flowsharp.Web.ViewComponents.ViewModels
{
    public class ActivityDescriptorViewModel : ShapeViewModel<ActivityDescriptor>
    {
        public ActivityDescriptorViewModel(ActivityDescriptor value) : base(value)
        {
        }
    }
}