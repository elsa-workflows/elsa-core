using Elsa.Connections.Credentials.WorkerProcess;

internal static class Program
{
    public static Task<int> Main(string[] args) => WorkerCommandHost.RunAsync(args);
}
