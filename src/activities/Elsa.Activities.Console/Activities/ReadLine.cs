using Elsa.Models;

namespace Elsa.Activities.Console.Activities
{
    /// <summary>
    /// Reads input from the console.
    /// </summary>
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