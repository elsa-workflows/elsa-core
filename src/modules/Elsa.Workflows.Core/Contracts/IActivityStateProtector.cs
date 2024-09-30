namespace Elsa.Workflows;

public interface IActivityStateProtector
{
    string Encode(ProtectedActivityStateContext context);
}

public record ProtectedActivityStateContext();