using Elsa.Activities.Email.Activities;
using Elsa.Web.Activities.Email.ViewModels;
using Elsa.Web.Components.ViewModels;
using Elsa.Web.Drivers;

namespace Elsa.Web.Activities.Email.Display
{
    public class SendEmailDisplay : ActivityDisplayDriver<SendEmail, SendEmailViewModel>
    {
        protected override void EditActivity(SendEmail activity, SendEmailViewModel model)
        {
            model.From = new ExpressionViewModel(activity.From);
            model.To = new ExpressionViewModel(activity.To);
            model.Subject = new ExpressionViewModel(activity.Subject);
            model.Body = new ExpressionViewModel(activity.Body);
        }

        protected override void UpdateActivity(SendEmailViewModel model, SendEmail activity)
        {
            activity.From = model.From.ToWorkflowExpression<string>();
            activity.To = model.To.ToWorkflowExpression<string>();
            activity.Subject = model.Subject.ToWorkflowExpression<string>();
            activity.Body = model.Body.ToWorkflowExpression<string>();
        }

        public SendEmailDisplay(IActivityDesignerStore designerStore) : base(designerStore)
        {
        }
    }
}