using System;
using Elsa.Testing.Shared.AutoFixture.Attributes;
using Elsa.Testing.Shared.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Core.IntegrationTests.Autofixture
{
    public class WithJavaScriptExpressionsAttribute : ElsaHostBuilderBuilderCustomizeAttributeBase
    {
        public override Action<ElsaHostBuilderBuilder> GetBuilderCustomizer()
        {
            return builder => {
                builder.ServiceCallbacks.Add(services => {
                    services.AddJavaScriptExpressionEvaluator();
                });
            };
        }
    }
}