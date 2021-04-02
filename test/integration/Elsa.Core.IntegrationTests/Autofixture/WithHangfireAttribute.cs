using System;
using Elsa.Testing.Shared.AutoFixture.Attributes;
using Elsa.Testing.Shared.Helpers;
using Hangfire;

namespace Elsa.Core.IntegrationTests.Autofixture
{
    public class WithHangfireAttribute : ElsaHostBuilderBuilderCustomizeAttributeBase
    {
        public override Action<ElsaHostBuilderBuilder> GetBuilderCustomizer()
        {
            return builder => {
                builder.ElsaCallbacks.Add(elsa => {
                    elsa.AddHangfireTemporalActivities(config => config.UseInMemoryStorage());
                });
            };
        }
    }
}