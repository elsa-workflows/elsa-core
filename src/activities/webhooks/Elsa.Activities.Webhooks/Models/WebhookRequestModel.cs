using System;
using System.Collections.Generic;

namespace Elsa.Activities.Webhooks.Models
{
    public record WebhookRequestModel(Uri Path, string Method, IDictionary<string, string> QueryString, IDictionary<string, string> Headers, object? Body = default)
    {
        public T GetBody<T>() => (T)Body!;
    }
}