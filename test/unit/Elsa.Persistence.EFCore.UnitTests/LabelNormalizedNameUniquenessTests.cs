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
    [Test]
    public async Task Label_HasUniqueIndex_OnTenantIdAndNormalizedName()
    {
        var builder = new ModelBuilder();
        new Configurations().Configure(builder.Entity<Label>());

        var index = builder.Model
            .FindEntityType(typeof(Label))!
            .GetIndexes()
            .Single(x => x.IsUnique);

        await Assert.That(index.Properties.Select(x => x.Name)).IsEquivalentTo(["TenantId", "NormalizedName"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(index.GetDatabaseName()).IsEqualTo("IX_Label_TenantId_NormalizedName");
    }

    [Test]
    public async Task Label_NameAndNormalizedName_HaveSharedMaxLength()
    {
        var builder = new ModelBuilder();
        new Configurations().Configure(builder.Entity<Label>());
        var entity = builder.Model.FindEntityType(typeof(Label))!;

        await Assert.That(entity.FindProperty(nameof(Label.Name))!.GetMaxLength()).IsEqualTo(Label.NameMaxLength);
        await Assert.That(entity.FindProperty(nameof(Label.NormalizedName))!.GetMaxLength()).IsEqualTo(Label.NameMaxLength);
        var sharedMaximum = Label.NameMaxLength;
        await Assert.That(sharedMaximum).IsEqualTo(255);
    }
}
