using System.Collections.Generic;
using Elsa.Attributes;
using Elsa.Models;

namespace Elsa.Activities.Primitives.Activities
{
    [Category("Control Flow")]
    [DisplayName("Join")]
    [Description("Join workflow execution back into a single path of execution.")]
    public class Join : Activity
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