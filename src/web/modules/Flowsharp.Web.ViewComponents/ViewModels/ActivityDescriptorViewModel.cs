using Flowsharp.Models;
using OrchardCore.DisplayManagement.Views;

namespace Flowsharp.Web.ViewComponents.ViewModels
{
    public class ActivityDescriptorViewModel : ShapeViewModel<Flowsharp.Models.IActivity>
    {
        public ActivityDescriptorViewModel(Flowsharp.Models.IActivity value) : base(value)
        {
        }
    }
}