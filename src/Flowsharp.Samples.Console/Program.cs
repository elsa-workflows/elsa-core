using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Samples.Console.Programs;

namespace Flowsharp.Samples.Console
{
    class Program
    {
        static async Task Main()
        {
            //await new AdditionWorkflowProgram().RunAsync(CancellationToken.None);
            await new AdditionWorkflowProgramLongRunning().RunAsync(CancellationToken.None);
        }
    }
}
