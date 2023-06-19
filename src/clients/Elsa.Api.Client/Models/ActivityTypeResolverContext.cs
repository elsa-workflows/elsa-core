using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;

namespace Elsa.Api.Client.Models;

/// <summary>
/// Provides context for an <see cref="IActivityTypeResolver"/>.
/// </summary>
/// <param name="ActivityTypeName">The activity type.</param>
public record ActivityTypeResolverContext(string ActivityTypeName);