using Elsa.Models;
using OrchardCore.DisplayManagement.Views;

namespace Elsa.Web.Components.ViewModels
{
    public class ActivityDescriptorViewModel : ShapeViewModel<ActivityDescriptor>
    {
        public ActivityDescriptorViewModel(ActivityDescriptor value) : base(value)
        {
        }
    }
}