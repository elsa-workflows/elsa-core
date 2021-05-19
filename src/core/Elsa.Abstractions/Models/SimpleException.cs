using System;

namespace Elsa.Models
{
    public class SimpleException
    {
        public SimpleException(Type type, string message, string stackTrace, SimpleException? innerException = default)
        {
            Type = type;
            Message = message;
            StackTrace = stackTrace;
            InnerException = innerException;
        }
        
        public Type Type { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public SimpleException? InnerException { get; set; }
        
        public static SimpleException? FromException(Exception? ex)
        {
            if (ex == null)
                return null;
            
            var simpleException = new SimpleException(ex.GetType(), ex.Message, ex.StackTrace);

            if (ex.InnerException != null)
                simpleException.InnerException = FromException(ex.InnerException);

            return simpleException;
        }
    }
}