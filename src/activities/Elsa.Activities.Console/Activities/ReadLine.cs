using Elsa.Attributes;
using Elsa.Models;

namespace Elsa.Activities.Console.Activities
{
    /// <summary>
    /// Reads input from the console.
    /// </summary>
    [ActivityDisplayName("Read Line")]
    [ActivityCategory("Console")]
    [ActivityDescription("Read a line from the console")]
    [DefaultEndpoint]
    public class ReadLine : Activity
    {
        public ReadLine()
        {
        }

        public ReadLine(string argumentName)
        {
            ArgumentName = argumentName;
        }
        
        public string ArgumentName { get; set; }
    }
}