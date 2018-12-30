using System.ComponentModel.DataAnnotations;

namespace Flowsharp.Web.Management.ViewModels
{
    public class CommonActivityViewModel
    {
        [Required]
        public string Title { get; set; }
    }
}