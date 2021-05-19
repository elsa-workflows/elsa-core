using System;
using AutoMapper;
using Elsa.Models;

namespace Elsa.Mapping
{
    public class ExceptionConverter : ITypeConverter<Exception, SimpleException>
    {
        public SimpleException Convert(Exception source, SimpleException? destination, ResolutionContext context)
        {
            var model = SimpleException.FromException(source)!;

            if (destination != null)
            {
                destination.Message = model.Message;
                destination.Type = model.Type;
                destination.StackTrace = model.StackTrace;
                destination.InnerException = model.InnerException;
            }
            else
            {
                destination = model;
            }

            return destination;
        }
    }
}