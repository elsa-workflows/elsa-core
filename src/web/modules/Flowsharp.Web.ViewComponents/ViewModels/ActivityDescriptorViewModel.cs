using Flowsharp.Models;
using OrchardCore.DisplayManagement.Views;

namespace Flowsharp.Web.ViewComponents.ViewModels
{
    public class ActivityDescriptorViewModel : ShapeViewModel<Flowsharp.Models.ActivityDescriptor>
    {
        public ActivityDescriptorViewModel(Flowsharp.Models.ActivityDescriptor value) : base(value)
        {
        }
    }
}