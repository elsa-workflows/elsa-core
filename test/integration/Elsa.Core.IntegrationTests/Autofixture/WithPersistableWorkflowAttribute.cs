using System;
using Elsa.Activities.UserTask.Activities;
using Elsa.Core.IntegrationTests.Workflows;
using Elsa.Testing.Shared.AutoFixture.Attributes;
using Elsa.Testing.Shared.Helpers;

namespace Elsa.Core.IntegrationTests.Autofixture
{
    public class WithPersistableWorkflowAttribute : ElsaHostBuilderBuilderCustomizeAttributeBase
    {
        public override Action<ElsaHostBuilderBuilder> GetBuilderCustomizer()
        {
            return builder => {
                builder.ElsaCallbacks.Add(elsa => {
                    elsa.AddActivity<UserTask>();
                    elsa.AddWorkflow<PersistableWorkflow>();
                });
            };
        }
    }
}