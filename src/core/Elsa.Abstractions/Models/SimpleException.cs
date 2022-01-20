using System;
using System.Collections;

namespace Elsa.Models
{
    public class SimpleException
    {
        public SimpleException(Type type, string message, string stackTrace, IDictionary data, SimpleException? innerException = default)
        {
            Type = type;
            Message = message;
            StackTrace = stackTrace;
            InnerException = innerException;
            Data = data;
        }
        
        public Type Type { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public SimpleException? InnerException { get; set; }
        public IDictionary Data { get; set; }
        
        public static SimpleException? FromException(Exception? ex)
        {
            if (ex == null)
                return null;

            var exceptionType = ex.GetType();
            var simpleException = new SimpleException(exceptionType, ex.Message, ex.StackTrace, ex.Data);

            if (ex.InnerException != null)
                simpleException.InnerException = FromException(ex.InnerException);

            return simpleException;
        }
    }
}