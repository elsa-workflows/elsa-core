using System;
using AutoMapper;
using Elsa.Models;

namespace Elsa.Mapping
{
    public class ExceptionProfile : Profile
    {
        public ExceptionProfile()
        {
            CreateMap<Exception, SimpleException>().ConvertUsing<ExceptionConverter>();
        }
    }
}