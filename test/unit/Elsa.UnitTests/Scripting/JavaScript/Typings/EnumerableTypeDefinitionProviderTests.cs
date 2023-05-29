using AutoFixture;
using AutoFixture.AutoMoq;
using Elsa.Scripting.JavaScript.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace Elsa.Scripting.JavaScript.Typings
{
    public class EnumerableTypeDefinitionProviderTests
    {
        [Theory(DisplayName = "The EnumerableTypeDefinitionProvider should return valid typescript definition even for complex types")]
        [InlineData(typeof(SomeComplexType[]), nameof(SomeComplexType))]
        [InlineData(typeof(List<SomeComplexType>), nameof(SomeComplexType))]
        [InlineData(typeof(Collection<SomeComplexType>), nameof(SomeComplexType))]
        [InlineData(typeof(List<string>), "string")]
        [InlineData(typeof(string[]), "string")]
        [InlineData(typeof(int[]), "number")]
        public void EnumerableTypeDefinitionProvider_if_provider_supports_collection_of_types(Type collectionType, string expectedElementType)
        {
            var serviceProvider = SetupTypeDefinitionProviders(new Fixture());

            var sut = new EnumerableTypeDefinitionProvider(serviceProvider);

            var result = sut.GetTypeDefinition(
                new TypeDefinitionContext(default, default),
                collectionType);

            Assert.Equal($"{expectedElementType}[]", result);
        }

        private static IServiceProvider SetupTypeDefinitionProviders(IFixture fixture)
        {
            fixture.Customize(new AutoMoqCustomization());
            var serviceProvider = fixture.Freeze<IServiceProvider>();
            Mock.Get(serviceProvider)
                .Setup(x => x.GetService(typeof(IEnumerable<ITypeDefinitionProvider>)))
                .Returns(() => new ITypeDefinitionProvider[] {
                    new BlacklistedTypeDefinitionProvider(),
                    new EnumTypeDefinitionProvider(),
                    new CommonTypeDefinitionProvider()
                });
            return serviceProvider;
        }
    }

    internal class SomeComplexType
    {
        public string Name { get; set; }
        public int Number { get; set; }
    }
}
