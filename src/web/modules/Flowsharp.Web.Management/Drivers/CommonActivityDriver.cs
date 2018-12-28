using Flowsharp.Web.Abstractions.Drivers;
using Flowsharp.Web.Management.ViewModels;
using OrchardCore.DisplayManagement.Views;

namespace Flowsharp.Web.Management.Drivers
{
    public class CommonActivityDriver : ActivityDisplayDriver<IActivity, CommonActivityViewModel>
    {
        public override IDisplayResult Display(IActivity model) => null;
        protected override string GetEditorShapeType(IActivity activity) => "CommonActivity_Edit";

        protected override void EditActivity(IActivity activity, CommonActivityViewModel model)
        {
            model.Title = activity.Metadata.Title;
        }

        protected override void UpdateActivity(CommonActivityViewModel model, IActivity activity)
        {
            activity.Metadata.Title = model.Title?.Trim();
        }
    }
}