using System.Collections.Generic;
using Elsa.Models;

namespace Elsa.Activities.Primitives.Activities
{
    public class Fork : ActivityBase
    {
        public IList<string> Forks { get; set; } = new List<string>();        
    }
}