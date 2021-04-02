using System;
using Elsa.Testing.Shared.AutoFixture.Attributes;
using Elsa.Testing.Shared.Helpers;

namespace Elsa.Core.IntegrationTests.Autofixture
{
    public class WithQuartzAttribute : ElsaHostBuilderBuilderCustomizeAttributeBase
    {
        public override Action<ElsaHostBuilderBuilder> GetBuilderCustomizer()
        {
            return builder => {
                builder.ElsaCallbacks.Add(elsa => {
                    elsa.AddQuartzTemporalActivities();
                });
            };
        }
    }
}