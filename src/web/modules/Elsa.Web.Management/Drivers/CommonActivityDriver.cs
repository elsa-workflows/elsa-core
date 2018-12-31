using Elsa.Web.Drivers;
using Elsa.Web.Management.ViewModels;
using OrchardCore.DisplayManagement.Views;

namespace Elsa.Web.Management.Drivers
{
    public class CommonActivityDriver : ActivityDisplayDriver<IActivity, CommonActivityViewModel>
    {
        public override IDisplayResult Display(IActivity model) => null;
        protected override string GetEditorShapeType(IActivity activity) => "CommonActivity_Edit";

        protected override void EditActivity(IActivity activity, CommonActivityViewModel model)
        {
            var title = activity.Metadata.Title;

            if (string.IsNullOrWhiteSpace(title))
            {
                title = activity.Descriptor.DisplayText.ToString();
            }
            
            model.Title = title;
        }

        protected override void UpdateActivity(CommonActivityViewModel model, IActivity activity)
        {
            activity.Metadata.Title = model.Title?.Trim();
        }
    }
}