using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Http.Models;

/// <summary>
/// A binding between an HTTP status code and an activity.
/// </summary>
public class HttpStatusCodeCase
{
    /// <summary>
    /// Creates a new instance of the <see cref="HttpStatusCodeCase"/> class.
    /// </summary>
    [JsonConstructor]
    public HttpStatusCodeCase()
    {
    }

    /// <summary>
    /// Creates a new instance of the <see cref="HttpStatusCodeCase"/> class.
    /// </summary>
    public HttpStatusCodeCase(int statusCode, IActivity activity)
    {
        StatusCode = statusCode;
        Activity = activity;
    }

    /// <summary>
    /// The HTTP status code to match.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// The activity to execute when the HTTP status code matches.
    /// </summary>
    public IActivity? Activity { get; set; }
}