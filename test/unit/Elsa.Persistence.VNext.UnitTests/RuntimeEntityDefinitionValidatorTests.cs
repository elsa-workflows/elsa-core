using Elsa.Persistence.VNext.Runtime;
using Elsa.Persistence.VNext.Runtime.Models;
using Elsa.Persistence.VNext.Runtime.Services;
using System.Threading.Tasks;

namespace Elsa.Persistence.VNext.UnitTests;

public class RuntimeEntityDefinitionValidatorTests
{
    private readonly RuntimeEntityDefinitionValidator _validator;

    public RuntimeEntityDefinitionValidatorTests()
    {
        _validator = new RuntimeEntityDefinitionValidator(Microsoft.Extensions.Options.Options.Create(new RuntimeEntityOptions()));
    }

    [Test]
    public async Task Validate_RejectsMissingDefinitionName()
    {
        var definition = CreateDefinition();
        definition.Name = " ";

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => _validator.Validate(definition));

        await Assert.That(exception.Message).Contains("name is required").WithComparison(StringComparison.CurrentCulture);
    }

    [Test]
    public async Task Validate_RejectsDefinitionWithoutFields()
    {
        var definition = CreateDefinition();
        definition.Fields.Clear();

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => _validator.Validate(definition));

        await Assert.That(exception.Message).Contains("must declare at least one field").WithComparison(StringComparison.CurrentCulture);
    }

    [Test]
    public async Task Validate_RejectsDuplicateFieldsIgnoringCase()
    {
        var definition = CreateDefinition();
        definition.Fields.Add(new("EMAIL", RuntimeEntityFieldType.String));

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => _validator.Validate(definition));

        await Assert.That(exception.Message).Contains("declares field").WithComparison(StringComparison.CurrentCulture);
        await Assert.That(exception.Message).Contains("more than once").WithComparison(StringComparison.CurrentCulture);
    }

    [Test]
    public async Task Validate_RejectsIndexesBeyondConfiguredLimit()
    {
        var validator = new RuntimeEntityDefinitionValidator(Microsoft.Extensions.Options.Options.Create(new RuntimeEntityOptions { MaxIndexedFields = 2 }));
        var definition = CreateDefinition();
        definition.Fields.Add(new("region", RuntimeEntityFieldType.String));
        definition.Indexes.Add(new("IX_Customer_Email", "email"));
        definition.Indexes.Add(new("IX_Customer_Tier", "tier"));
        definition.Indexes.Add(new("IX_Customer_Region", "region"));

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => validator.Validate(definition));

        await Assert.That(exception.Message).Contains("declares 3 indexes").WithComparison(StringComparison.CurrentCulture);
        await Assert.That(exception.Message).Contains("only 2 runtime index slots").WithComparison(StringComparison.CurrentCulture);
    }

    [Test]
    public async Task Validate_RejectsIndexesBeyondRuntimeSlotCount()
    {
        var validator = new RuntimeEntityDefinitionValidator(Microsoft.Extensions.Options.Options.Create(new RuntimeEntityOptions { MaxIndexedFields = 100 }));
        var definition = CreateDefinition();

        for (var i = 1; i <= RuntimeEntityPersistenceSchemaProvider.IndexedFieldSlotCount + 1; i++)
        {
            var fieldName = $"indexed{i}";
            definition.Fields.Add(new(fieldName, RuntimeEntityFieldType.String));
            definition.Indexes.Add(new($"IX_Customer_{fieldName}", fieldName));
        }

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => validator.Validate(definition));

        await Assert.That(exception.Message).Contains($"only {RuntimeEntityPersistenceSchemaProvider.IndexedFieldSlotCount} runtime index slots").WithComparison(StringComparison.CurrentCulture);
    }

    [Test]
    public async Task Validate_RejectsIndexReferencingUnknownField()
    {
        var definition = CreateDefinition();
        definition.Indexes.Add(new("IX_Customer_Status", "status"));

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => _validator.Validate(definition));

        await Assert.That(exception.Message).Contains("references unknown field 'status'").WithComparison(StringComparison.CurrentCulture);
    }

    [Test]
    public async Task ValidateInstance_RejectsMissingRequiredField()
    {
        var definition = CreateDefinition();
        var instance = new RuntimeEntityInstance
        {
            Id = "customer-1",
            DefinitionName = definition.Name
        };

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => _validator.ValidateInstance(definition, instance));

        await Assert.That(exception.Message).Contains("missing required field 'email'").WithComparison(StringComparison.CurrentCulture);
    }

    [Test]
    public void ValidateInstance_MatchesRequiredFieldsIgnoringCase()
    {
        var definition = CreateDefinition();
        var instance = new RuntimeEntityInstance
        {
            Id = "customer-1",
            DefinitionName = definition.Name,
            Data =
            {
                ["EMAIL"] = "one@example.com"
            }
        };

        _validator.ValidateInstance(definition, instance);
    }

    private static RuntimeEntityDefinition CreateDefinition()
    {
        return new RuntimeEntityDefinition
        {
            Name = "Customer",
            Fields =
            {
                new("email", RuntimeEntityFieldType.String, IsRequired: true),
                new("tier", RuntimeEntityFieldType.String)
            }
        };
    }
}
