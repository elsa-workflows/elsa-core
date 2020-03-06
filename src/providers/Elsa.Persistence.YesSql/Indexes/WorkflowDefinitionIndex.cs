using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Persistence.YesSql.Documents;
using YesSql.Indexes;

namespace Elsa.Persistence.YesSql.Indexes
{
    public class WorkflowDefinitionIndex : MapIndex
    {
        public string Id { get; set; }
        public string TenantId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class WorkflowDefinitionIndexProvider : IndexProvider<WorkflowDefinitionDocument>
    {
        public override void Describe(DescribeContext<WorkflowDefinitionDocument> context)
        {
            context.For<WorkflowDefinitionIndex>()
                .Map(
                    document => new WorkflowDefinitionIndex
                    {
                        Id = document.Id,
                        TenantId = document.TenantId,
                        CreatedAt = document.CreatedAt
                    }
                );

        }
    }
}