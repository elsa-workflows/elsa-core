using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Elsa.Web.Components.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Elsa.Web.Activities.Http.ViewModels
{
    public class HttpRequestActionViewModel
    {
        public ExpressionViewModel Url { get; set; }

        [Required]
        public string Method { get; set; }

        public ExpressionViewModel Body { get; set; }

        public ExpressionViewModel ContentType { get; set; }
        public ExpressionViewModel RequestHeaders { get; set; }
        
        [Required]
        public string SupportedStatusCodes { get; set; }

        public ICollection<SelectListItem> GetAvailableHttpMethods()
        {
            var availableHttpMethods = new[] { "GET", "POST", "PUT", "DELETE", "OPTIONS" };
            return availableHttpMethods.Select(x => new SelectListItem { Text = x, Value = x }).ToList();
        }
    }
}