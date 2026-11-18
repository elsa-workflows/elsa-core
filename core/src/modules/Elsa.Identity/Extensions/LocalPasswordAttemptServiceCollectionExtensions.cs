using Elsa.Identity.Contracts;
using Elsa.Identity.Options;
using Elsa.Identity.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Identity.Extensions;

internal static class LocalPasswordAttemptServiceCollectionExtensions
{
    public static void AddLocalPasswordAttemptLimiter(this IServiceCollection services)
    {
        services.AddOptions<LocalPasswordAttemptOptions>()
            .Validate(options => options.AccountLimit > 0 && options.SourceLimit > 0 && options.PartitionLimit > 0 &&
                                 options.Window > TimeSpan.Zero && options.Window <= TimeSpan.FromDays(1),
                "Local password attempt limits must be positive and the window must be no longer than one day.")
            .ValidateOnStart();
        services.TryAddSingleton<ILocalPasswordAttemptLimiter, MemoryLocalPasswordAttemptLimiter>();
    }
}
