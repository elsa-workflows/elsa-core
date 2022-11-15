using System.Reflection;
using Elsa.Telnyx.Attributes;
using Elsa.Telnyx.Payloads.Abstract;

namespace Elsa.Telnyx.Helpers;

/// <summary>
/// A helper class related to webhook event payload discovery. 
/// </summary>
public static class WebhookPayloadTypes
{
    /// <summary>
    /// A list of <see cref="Payload"/> types.
    /// </summary>
    public static readonly ICollection<Type> PayloadTypes;

    static WebhookPayloadTypes()
    {
        PayloadTypes = typeof(WebhookPayloadTypes).Assembly.GetTypes().Where(x => typeof(Payload).IsAssignableFrom(x) && x.GetCustomAttribute<WebhookAttribute>() != null).ToList();
    }
}