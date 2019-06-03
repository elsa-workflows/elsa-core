using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Elsa.Activities.Http.Web.ViewModels
{
    public class HttpRequestTriggerViewModel
    {
        [Required]
        public Uri Path { get; set; }

        [Required]
        public string Method { get; set; }

        public bool ReadContent { get; set; }

        public ICollection<SelectListItem> GetAvailableHttpMethods()
        {
            var availableHttpMethods = new[] { "GET", "POST", "PUT", "DELETE", "OPTIONS" };
            return availableHttpMethods.Select(x => new SelectListItem { Text = x, Value = x }).ToList();
        }
    }
}