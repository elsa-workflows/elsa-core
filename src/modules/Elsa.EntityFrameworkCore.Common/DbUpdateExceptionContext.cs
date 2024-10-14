namespace Elsa.EntityFrameworkCore;

public record DbUpdateExceptionContext(Exception Exception, CancellationToken CancellationToken);