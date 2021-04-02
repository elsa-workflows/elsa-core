using System.Reflection;
using AutoFixture;
using AutoFixture.Xunit2;
using Elsa.Core.IntegrationTests.Workflows;
using Elsa.Persistence.MongoDb.Extensions;
using Elsa.Testing.Shared.AutoFixture.Customizations;
using Microsoft.Extensions.DependencyInjection;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Elsa.Persistence.EntityFramework.Sqlite;
using Elsa.Persistence.YesSql;
using YesSql.Provider.Sqlite;
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