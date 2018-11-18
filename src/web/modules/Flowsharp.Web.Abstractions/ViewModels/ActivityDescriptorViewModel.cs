using Flowsharp.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using OrchardCore.DisplayManagement.Views;

namespace Flowsharp.Web.Abstractions.ViewModels
{
    public class ActivityViewModel : ShapeViewModel
    {
        public ActivityViewModel()
        {
        }

        public ActivityViewModel(IActivity activity)
        {
            Activity = activity;
        }

        [BindNever]
        public IActivity Activity { get; set; }
    }
}