namespace Elsa.Workflows.Admission.WorkerProcess;

internal static class AdmissionProgram
{
    public static Task<int> Main(string[] args) => AdmissionWorkerHost.RunAsync(args);
}
