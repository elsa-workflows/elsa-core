using System;
using System.Reflection;
using AutoFixture;
using AutoFixture.Xunit2;
using Moq;

namespace Elsa.Scripting.JavaScript.Services
{
    public class ServiceProviderForConvertersAttribute : CustomizeAttribute
    {
        public override ICustomization GetCustomization(ParameterInfo parameter)
            => new ServiceProviderForConvertersCustomization();

        internal class ServiceProviderForConvertersCustomization : ICustomization
        {
            public void Customize(IFixture fixture)
            {
                var serviceProvider = fixture.Freeze<IServiceProvider>();
                Mock.Get(serviceProvider)
                    .Setup(x => x.GetService(typeof(IConvertsEnumerableToObject)))
                    .Returns(() => new EnumerableResultConverter(default));
            }
        }
    }
}