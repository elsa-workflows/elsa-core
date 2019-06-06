using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Web.Drivers;
using Elsa.Web.Management.ViewModels;
using OrchardCore.DisplayManagement.Views;

namespace Elsa.Web.Management.Drivers
{
    public class CommonActivityDriver : ActivityDisplayDriver<IActivity, CommonActivityViewModel>
    {
        public override IDisplayResult Display(IActivity model) => null;
        protected override string GetEditorShapeType(IActivity activity) => "CommonActivity_Edit";

        protected override async Task EditActivityAsync(IActivity activity, CommonActivityViewModel model)
        {
            var title = activity.Metadata.Title;

            if (string.IsNullOrWhiteSpace(title))
            {
                var designer = await DesignerStore.GetByTypeNameAsync(activity.TypeName, CancellationToken.None);
                title = designer.DisplayName.ToString();
            }
            
            model.Title = title;
        }

        protected override void UpdateActivity(CommonActivityViewModel model, IActivity activity)
        {
            activity.Metadata.Title = model.Title?.Trim();
        }

        public CommonActivityDriver(IActivityDesignerStore designerStore) : base(designerStore)
        {
        }
    }
}