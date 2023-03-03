namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Implemented by trigger activities that generate events. For example, a timer activity is an event generator. 
/// </summary>
/// <remarks>
/// When an event generator activity is marked as startable, it generates new workflow instances. Otherwise, it schedules new workflow executions for the current workflow instance, regardless of whether or not the activity is currently blocking.
/// For example, a timer activity will continuously generate events, causing the workflow instance to execute the timer event and subsequent activities, even if the timer activity wasn't scheduled with the workflow execution stack.    
/// </remarks>
public interface IEventGenerator : ITrigger
{
}