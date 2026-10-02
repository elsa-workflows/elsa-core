using System.Diagnostics.CodeAnalysis;
using Elsa.Api.Client.Extensions;

namespace Elsa.Studio.Services;

/// <summary>
/// Bridges calls to <c>Elsa.Api.Client</c> API client creation methods while preserving trimming annotations.
/// </summary>
internal static class ApiClientFactory
{
    internal static T Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(IServiceProvider serviceProvider, Uri backendUrl) where T : class
        => serviceProvider.CreateApi<T>(backendUrl);
}

