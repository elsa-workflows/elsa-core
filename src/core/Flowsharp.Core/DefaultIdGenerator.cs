using System;
using Flowsharp.Web.Abstractions.Services;

namespace Flowsharp.Web.Management.Services
{
    public class DefaultIdGenerator : IIdGenerator
    {
        public string Generate() => Guid.NewGuid().ToString("N");
    }
}