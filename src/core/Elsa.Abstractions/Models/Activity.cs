using System;
using System.ComponentModel.DataAnnotations;

namespace Elsa.Models
{
    public abstract class Activity : IActivity
    {
        [ScaffoldColumn(false)]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        
        [ScaffoldColumn(false)]
        public string TypeName => GetType().Name;
        
        [Display(Description = "Optionally provide a shorthand name for this specific activity that you can use to reference this activity from script.")]
        public string Alias { get; set; }
        
        [ScaffoldColumn(false)]
        public ActivityMetadata Metadata { get; set; } = new ActivityMetadata();
    }
}
