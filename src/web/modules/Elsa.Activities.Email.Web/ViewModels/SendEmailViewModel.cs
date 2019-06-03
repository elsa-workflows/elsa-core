using Elsa.Web.Components.ViewModels;

namespace Elsa.Activities.Email.Web.ViewModels
{
    public class SendEmailViewModel
    {
        public ExpressionViewModel From { get; set; }
        public ExpressionViewModel To { get; set; }
        public ExpressionViewModel Subject { get; set; }
        public ExpressionViewModel Body { get; set; }
    }
}