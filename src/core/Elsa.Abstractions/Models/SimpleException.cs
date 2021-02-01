using System;

namespace Elsa.Models
{
    public record SimpleException(Type Type, string Message, string StackTrace, SimpleException? InnerException = default)
    {
        public static SimpleException? FromException(Exception? ex)
        {
            if (ex == null)
                return null;
            
            var simpleException = new SimpleException(ex.GetType(), ex.Message, ex.StackTrace);

            if (ex.InnerException != null)
                simpleException = simpleException with { InnerException = FromException(ex.InnerException) };

            return simpleException;
        }
    }
}