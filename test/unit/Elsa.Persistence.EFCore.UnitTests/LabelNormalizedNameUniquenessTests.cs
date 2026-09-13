using Elsa.Labels.Entities;
using Elsa.Persistence.EFCore.Modules.Labels;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.UnitTests;

/// <summary>
/// The Labels unique index must be per tenant, matching Secrets'
/// <c>IX_Secret_TenantId_NormalizedName</c>. A global unique index would make
/// the name a shared resource: the second tenant could not create "urgent".
/// </summary>
public class LabelNormalizedNameUniquenessTests
{
    [Fact]
    public void Label_HasUniqueIndex_OnTenantIdAndNormalizedName()
    {
        var builder = new ModelBuilder();
        new Configurations().Configure(builder.Entity<Label>());

        var index = builder.Model
            .FindEntityType(typeof(Label))!
            .GetIndexes()
            .Single(x => x.IsUnique);

        Assert.Equal(["TenantId", "NormalizedName"], index.Properties.Select(x => x.Name));
        Assert.Equal("IX_Label_TenantId_NormalizedName", index.GetDatabaseName());
    }
}
