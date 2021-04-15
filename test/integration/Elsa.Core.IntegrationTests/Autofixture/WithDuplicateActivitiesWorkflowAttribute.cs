using Elsa.Core.IntegrationTests.Workflows;
using Elsa.Testing.Shared.Helpers;
using Elsa.Testing.Shared.AutoFixture.Attributes;
using System;

namespace Elsa.Core.IntegrationTests.Autofixture
{
    public class WithDuplicateActivitiesWorkflowAttribute : ElsaHostBuilderBuilderCustomizeAttributeBase
    {
        public override Action<ElsaHostBuilderBuilder> GetBuilderCustomizer()
        {
            return builder => {
                builder.ElsaCallbacks.Add(elsa => {
                    elsa.AddWorkflow<DuplicateActivitiesWorkflow>();
                });
            };
        }
    }
}