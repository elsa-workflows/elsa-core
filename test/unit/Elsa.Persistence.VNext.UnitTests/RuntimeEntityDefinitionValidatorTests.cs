using Elsa.Persistence.VNext.Runtime;
using Elsa.Persistence.VNext.Runtime.Models;
using Elsa.Persistence.VNext.Runtime.Services;

namespace Elsa.Persistence.VNext.UnitTests;

public class RuntimeEntityDefinitionValidatorTests
{
    private readonly RuntimeEntityDefinitionValidator _validator;

    public RuntimeEntityDefinitionValidatorTests()
    {
        _validator = new RuntimeEntityDefinitionValidator(Microsoft.Extensions.Options.Options.Create(new RuntimeEntityOptions()));
    }

    [Fact]
    public void Validate_RejectsMissingDefinitionName()
    {
        var definition = CreateDefinition();
        definition.Name = " ";

        var exception = Assert.Throws<InvalidOperationException>(() => _validator.Validate(definition));

        Assert.Contains("name is required", exception.Message);
    }

    [Fact]
    public void Validate_RejectsDefinitionWithoutFields()
    {
        var definition = CreateDefinition();
        definition.Fields.Clear();

        var exception = Assert.Throws<InvalidOperationException>(() => _validator.Validate(definition));

        Assert.Contains("must declare at least one field", exception.Message);
    }

    [Fact]
    public void Validate_RejectsDuplicateFieldsIgnoringCase()
    {
        var definition = CreateDefinition();
        definition.Fields.Add(new("EMAIL", RuntimeEntityFieldType.String));

        var exception = Assert.Throws<InvalidOperationException>(() => _validator.Validate(definition));

        Assert.Contains("declares field", exception.Message);
        Assert.Contains("more than once", exception.Message);
    }

    [Fact]
    public void Validate_RejectsIndexesBeyondConfiguredLimit()
    {
        var validator = new RuntimeEntityDefinitionValidator(Microsoft.Extensions.Options.Options.Create(new RuntimeEntityOptions { MaxIndexedFields = 2 }));
        var definition = CreateDefinition();
        definition.Fields.Add(new("region", RuntimeEntityFieldType.String));
        definition.Indexes.Add(new("IX_Customer_Email", "email"));
        definition.Indexes.Add(new("IX_Customer_Tier", "tier"));
        definition.Indexes.Add(new("IX_Customer_Region", "region"));

        var exception = Assert.Throws<InvalidOperationException>(() => validator.Validate(definition));

        Assert.Contains("declares 3 indexes", exception.Message);
        Assert.Contains("only 2 runtime index slots", exception.Message);
    }

    [Fact]
    public void Validate_CapsIndexesAtRuntimeSlotCount()
    {
        var validator = new RuntimeEntityDefinitionValidator(Microsoft.Extensions.Options.Options.Create(new RuntimeEntityOptions { MaxIndexedFields = 100 }));
        var definition = CreateDefinition();

        for (var i = 1; i <= RuntimeEntityPersistenceSchemaProvider.IndexedFieldSlotCount + 1; i++)
        {
            var fieldName = $"indexed{i}";
            definition.Fields.Add(new(fieldName, RuntimeEntityFieldType.String));
            definition.Indexes.Add(new($"IX_Customer_{fieldName}", fieldName));
        }

        var exception = Assert.Throws<InvalidOperationException>(() => validator.Validate(definition));

        Assert.Contains($"only {RuntimeEntityPersistenceSchemaProvider.IndexedFieldSlotCount} runtime index slots", exception.Message);
    }

    [Fact]
    public void Validate_RejectsIndexReferencingUnknownField()
    {
        var definition = CreateDefinition();
        definition.Indexes.Add(new("IX_Customer_Status", "status"));

        var exception = Assert.Throws<InvalidOperationException>(() => _validator.Validate(definition));

        Assert.Contains("references unknown field 'status'", exception.Message);
    }

    [Fact]
    public void ValidateInstance_RejectsMissingRequiredField()
    {
        var definition = CreateDefinition();
        var instance = new RuntimeEntityInstance
        {
            Id = "customer-1",
            DefinitionName = definition.Name
        };

        var exception = Assert.Throws<InvalidOperationException>(() => _validator.ValidateInstance(definition, instance));

        Assert.Contains("missing required field 'email'", exception.Message);
    }

    [Fact]
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
