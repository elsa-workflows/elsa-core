namespace Elsa.Jobs.Services;

internal interface IScheduledJob
{
    string Name { get; set; }
    void Cancel();
}