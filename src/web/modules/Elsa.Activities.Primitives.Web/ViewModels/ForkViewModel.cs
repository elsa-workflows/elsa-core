using System.ComponentModel.DataAnnotations;

namespace Elsa.Activities.Primitives.Web.ViewModels
{
    public class ForkViewModel
    {
        [Required]
        public string Forks { get; set; }
    }
}