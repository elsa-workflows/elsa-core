using System.ComponentModel.DataAnnotations;

namespace Elsa.Web.Management.ViewModels
{
    public class CommonActivityViewModel
    {
        [Required]
        public string Title { get; set; }
    }
}