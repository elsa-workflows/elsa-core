using Elsa.Web.Components.ViewModels;

namespace Elsa.Web.Activities.Email.ViewModels
{
    public class SendEmailViewModel
    {
        public ExpressionViewModel From { get; set; }
        public ExpressionViewModel To { get; set; }
        public ExpressionViewModel Subject { get; set; }
        public ExpressionViewModel Body { get; set; }
    }
}