using System.Collections.Generic;

namespace Flowsharp.Models
{
    public class Variables : Dictionary<string, object>
    {        
        public static readonly Variables Empty = new Variables();
    }
}