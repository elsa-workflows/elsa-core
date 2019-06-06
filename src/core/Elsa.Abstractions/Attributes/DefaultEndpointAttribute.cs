using System;

namespace Elsa.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DefaultEndpointAttribute : EndpointsAttribute
    {
        public DefaultEndpointAttribute() : base(EndpointNames.Done)
        {
        }
    }
}