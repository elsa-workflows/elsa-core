using Microsoft.Extensions.Options;

namespace Elsa.Common.Multitenancy;

internal sealed class TenantActivationOptionsValidator : IValidateOptions<TenantActivationOptions>
{
    public ValidateOptionsResult Validate(string? name, TenantActivationOptions options)
    {
        return options.MaxDegreeOfParallelism == -1 || options.MaxDegreeOfParallelism >= 1
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail("TenantActivationOptions.MaxDegreeOfParallelism must be -1 (no limit) or at least 1.");
    }
}
