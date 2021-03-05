using System;
using System.IO;
using Elsa;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptions AddWebhooksActivities(this ElsaOptions options)
        {
            return options;
        }
    }
}
