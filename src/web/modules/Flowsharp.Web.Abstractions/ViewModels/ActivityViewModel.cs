using Microsoft.AspNetCore.Mvc.ModelBinding;
using OrchardCore.DisplayManagement.Views;

namespace Flowsharp.Web.Abstractions.ViewModels
{
    public class ActivityViewModel<TActivity> : ShapeViewModel where TActivity : IActivity
    {
        public ActivityViewModel()
        {
        }

        public ActivityViewModel(TActivity activity)
        {
            Activity = activity;
        }

        [BindNever]
        public TActivity Activity { get; set; }
    }
}
