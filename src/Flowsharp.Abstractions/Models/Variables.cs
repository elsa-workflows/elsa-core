using System.Collections.Generic;
using Newtonsoft.Json;

namespace Flowsharp.Models
{
    public class Variables : Dictionary<string, object>
    {        
        public static readonly Variables Empty = new Variables();
    }
}