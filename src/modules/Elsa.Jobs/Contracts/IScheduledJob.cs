namespace Elsa.Jobs.Contracts;

internal interface IScheduledJob
{
    string Name { get; set; }
    void Cancel();
}