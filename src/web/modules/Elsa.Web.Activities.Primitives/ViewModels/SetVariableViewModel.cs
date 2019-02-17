using System.ComponentModel.DataAnnotations;
using Elsa.Web.Components.ViewModels;

namespace Elsa.Web.Activities.Primitives.ViewModels
{
    public class SetVariableViewModel
    {
        [Required]
        public string VariableName { get; set; }
        public ExpressionViewModel ValueExpression { get; set; }
    }
}