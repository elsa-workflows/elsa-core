using AutoMapper;
using Elsa.Persistence.Specifications;
using Elsa.Persistence;
using Elsa.Server.Api.Mapping;
using Elsa.Services;
using Elsa.Server.Api.Endpoints.WorkflowDefinitions;
using Elsa.Server.Api.Test.Mock;
using Moq;
using Elsa.Models;

namespace Elsa.Server.Api.Test
{
    public class TestBase
    {
        public IMapper Mapper;

        public TestBase()
        {
            SetupMapper();
        }

        public List Setup(string tenantId, List<WorkflowDefinition> workflowDefinitions)
        {
            var workflowDefinitionStore = new Mock<IWorkflowDefinitionStore>();
            var tenantAcessorMoq = new Mock<ITenantAccessor>();
            IQueryable<WorkflowDefinition> filteredResult = null;

            workflowDefinitionStore.Setup(x => x.FindManyAsync(
                It.IsAny<ISpecification<WorkflowDefinition>>(),
                It.IsAny<IOrderBy<WorkflowDefinition>>(),
                It.IsAny<IPaging>(),
                It.IsAny<CancellationToken>())
            ).Callback((ISpecification<WorkflowDefinition> specification, IOrderBy<WorkflowDefinition> orderBy, IPaging paging, CancellationToken cancellationToken) =>
            {
                filteredResult = workflowDefinitions.AsQueryable();

                filteredResult = filteredResult.Apply(specification).Apply(orderBy);
            })
            .ReturnsAsync(() => filteredResult.ToList());

            workflowDefinitionStore.Setup(x => x.CountAsync(
              It.IsAny<ISpecification<WorkflowDefinition>>(),
              It.IsAny<CancellationToken>())
            ).Callback((ISpecification<WorkflowDefinition> specification, CancellationToken cancellationToken) =>
            {
                filteredResult = workflowDefinitions.AsQueryable();

                filteredResult = filteredResult.Apply(specification);
            })
            .ReturnsAsync(() => filteredResult.Count());

            tenantAcessorMoq.Setup(x => x.GetTenantIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(tenantId);


            return new List(workflowDefinitionStore.Object, Mapper, tenantAcessorMoq.Object);
        }

        /// <summary>
        /// Setup Mapping dependencies
        /// </summary>
        /// <returns></returns>
        private void SetupMapper()
        {

            // Setup mapper
            var myProfile = new AutoMapperProfile();
            var configuration = new MapperConfiguration(cfg => cfg.AddProfile(myProfile));
            Mapper = new AutoMapper.Mapper(configuration);
        }

    }
}
