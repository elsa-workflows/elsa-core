// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Signaling
{
    public enum SignalScope
    {
        /// <summary>
        /// Only signals targeting a specific workflow instance will be handled.
        /// </summary>
        Instance,
        
        /// <summary>
        /// All signals with a specified name will be handled.
        /// </summary>
        Global
    }
}