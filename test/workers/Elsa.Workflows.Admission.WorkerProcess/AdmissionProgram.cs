namespace Elsa.Workflows.Admission.WorkerProcess;

internal static class AdmissionProgram
{
    public static Task<int> Main(string[] args) => args.Length > 0 && args[0].StartsWith("runtime-", StringComparison.Ordinal)
        ? AdmissionRuntimeCommands.RunAsync(args)
        : AdmissionWorkerHost.RunAsync(args);
}
