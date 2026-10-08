namespace Elsa.Persistence.EFCore;

public record DbUpdateExceptionContext(Exception Exception, CancellationToken CancellationToken);