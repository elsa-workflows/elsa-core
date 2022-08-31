namespace Elsa.Models;

public record ValidateCredentialsResult(bool IsValid, object? Context = null)
{
    public static ValidateCredentialsResult Invalid() => new(false);
    public static ValidateCredentialsResult Valid(object? context = null) => new(true, context);
}