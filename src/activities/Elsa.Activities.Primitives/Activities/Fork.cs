using System.Collections.Generic;
using Elsa.Attributes;
using Elsa.Models;

namespace Elsa.Activities.Primitives.Activities
{
    [Category("Control Flow")]
    [DisplayName("Fork")]
    [Description("Fork workflow execution into separate paths of execution.")]
    [DefaultEndpoint]
    public class Fork : Activity
    {
        public IList<string> Forks { get; set; } = new List<string>();        
    }
}