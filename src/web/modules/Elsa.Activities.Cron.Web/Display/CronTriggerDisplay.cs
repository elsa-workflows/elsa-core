using Elsa.Activities.Cron.Activities;
using Elsa.Activities.Cron.Web.ViewModels;
using Elsa.Web.Components.ViewModels;
using Elsa.Web.Drivers;

namespace Elsa.Activities.Cron.Web.Display
{
    public class CronTriggerDisplay : ActivityDisplayDriver<CronTrigger, CronTriggerViewModel>
    {
        protected override void EditActivity(CronTrigger activity, CronTriggerViewModel model)
        {
            model.CronExpression = new ExpressionViewModel(activity.CronExpression);
        }

        protected override void UpdateActivity(CronTriggerViewModel model, CronTrigger activity)
        {
            activity.CronExpression = model.CronExpression.ToWorkflowExpression<string>();
        }

        public CronTriggerDisplay(IActivityDesignerStore designerStore) : base(designerStore)
        {
        }
    }
}