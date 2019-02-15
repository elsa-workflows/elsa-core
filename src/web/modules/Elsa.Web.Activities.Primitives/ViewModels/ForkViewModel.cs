using System.ComponentModel.DataAnnotations;

namespace Elsa.Web.Activities.Primitives.ViewModels
{
    public class ForkViewModel
    {
        [Required]
        public string Forks { get; set; }
    }
}