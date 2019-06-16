using System.Collections.Generic;
using Elsa.Models;

namespace Elsa.Activities.Primitives.Activities
{
    public class Join : ActivityBase
    {
        public enum JoinMode
        {
            WaitAll,
            WaitAny
        }        
        
        public JoinMode Mode { get; set; }
        public IList<string> InboundTransitions { get; set; } = new List<string>();
    }
}