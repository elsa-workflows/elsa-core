using Xunit;
using Moq;
using Elsa.Server.Api.Endpoints.WorkflowDefinitions;
using Elsa.Persistence;
using Elsa.Services;
using Elsa.Server.Api.Models;
using Elsa.Persistence.Specifications;
using Elsa.Models;
using NodaTime;
using Elsa.Server.Api.Test.Mock;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Server.Api.Test.WorkflowDefinitions
{
    public class WorkflowDefinitionTest : TestBase
    {
        private List<WorkflowDefinition> GetWorkflowDefinitions(string tenantId)
        {
            Instant now = SystemClock.Instance.GetCurrentInstant();
            ZonedDateTime nowInIsoUtc = now.InUtc();

            var lastWeek = nowInIsoUtc.PlusHours(-(24 * 7));
            var yesterday = nowInIsoUtc.PlusHours(-24);
            var workflowDefinitions = new List<WorkflowDefinition>
            {
                new WorkflowDefinition
                {
                    CreatedAt = yesterday.ToInstant(),
                    Name = "B",
                    Description= "Hard",
                    TenantId = tenantId,
                    IsLatest = true
                },
                new WorkflowDefinition
                {
                    CreatedAt = nowInIsoUtc.ToInstant(),
                    Name = "A",
                    Description= "Normal",
                    TenantId = tenantId,
                    IsLatest = true
                },
                new WorkflowDefinition
                {
                    CreatedAt = lastWeek.ToInstant(),
                    Name = "C",
                    Description= "Easy",
                    TenantId = tenantId,
                    IsLatest = true
                }
            };

            return workflowDefinitions;
        }

        [Fact]
        public async Task Given_FetchingAList_When_NoParameterIsSpecified_Then_OrderedResultsByNameAscendingAreReturned()
        {
            //Arrange
            var tenantId = "tenant001";

            var workflowDefinitions = GetWorkflowDefinitions(tenantId);

            var list = Setup(tenantId, workflowDefinitions);

            //Act
            var actionResult =  await list.Handle(null);

            //Assert
            var jsonResult = Assert.IsType<JsonResult>(actionResult.Result);
            var result = Assert.IsType<PagedList<WorkflowDefinitionSummaryModel>>(jsonResult.Value);
            var items = result?.Items?.ToList();

            Assert.NotNull(items);
            Assert.NotEmpty(items);
            Assert.True(items[0].Name == workflowDefinitions[1].Name);
            Assert.True(items[1].Name == workflowDefinitions[0].Name);
            Assert.True(items[2].Name == workflowDefinitions[2].Name);
        }

    }
}
