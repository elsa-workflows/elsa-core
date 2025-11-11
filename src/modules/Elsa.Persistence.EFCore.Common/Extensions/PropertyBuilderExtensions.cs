using Elsa.Persistence.EFCore.Converters;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Persistence.EFCore.Extensions;

public static class PropertyBuilderExtensions {

    /// <summary>
    /// Serializes field as JSON blob in database.
    /// </summary>
    public static PropertyBuilder<T> HasJsonValueConversion<T>(this PropertyBuilder<T> propertyBuilder) where T : class {

        propertyBuilder
            .HasConversion(new JsonValueConverter<T>())
            .Metadata.SetValueComparer(new JsonValueComparer<T>());

        return propertyBuilder;

    }

}