using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Activities;
using Flowsharp.Models;
using Flowsharp.Results;
using Newtonsoft.Json;

namespace Flowsharp.Samples.Console.Activities
{
    public class ReadLine : Activity
    {
        public string ArgumentName { get; set; }
    }
}