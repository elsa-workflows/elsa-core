using Elsa.Api.Client.Activities;

namespace Elsa.Api.Client.Models;

/// <summary>
/// Represents an expected status code in the FlowSendHttpRequest activity.
/// </summary>
public class HttpStatusCodeCase
{
    /// <summary>
    /// The HTTP status code to match.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// The activity to execute when the HTTP status code matches.
    /// </summary>
    public Activity? Activity { get; set; }
}