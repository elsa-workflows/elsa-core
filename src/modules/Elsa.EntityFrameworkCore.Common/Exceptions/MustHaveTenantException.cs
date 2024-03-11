namespace Elsa.EntityFrameworkCore.Common.Exceptions;

public class MustHaveTenantException : Exception
{
    public MustHaveTenantException() : base()
    {
    }

    public MustHaveTenantException(string? message) : base(message)
    {
    }

    public MustHaveTenantException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
