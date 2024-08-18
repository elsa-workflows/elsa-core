namespace Elsa.EntityFrameworkCore.Common;

public record DbUpdateExceptionContext(Exception Exception, CancellationToken CancellationToken);