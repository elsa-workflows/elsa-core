using System.Text.Json;
using Elsa.ExternalAuthentication.Models;

namespace Elsa.ExternalAuthentication.Permissions;

internal static class BuiltInPermissionGrantSourceDescriptors
{
    public static IReadOnlyList<SettingFieldDescriptor> Mapping(string defaultClaimType) =>
    [
        Field(
            "claimType",
            "Projected claim type",
            "The normalized projected claim whose values are matched.",
            "string",
            "text",
            JsonSerializer.SerializeToElement(defaultClaimType)),
        Field(
            "mappings",
            "Claim value mappings",
            "A JSON object or array mapping explicit claim values to arrays of Elsa permission strings.",
            "json",
            "json",
            JsonSerializer.SerializeToElement(Array.Empty<object>()))
    ];

    public static IReadOnlyList<SettingFieldDescriptor> PassThrough() =>
    [
        Field(
            "claimType",
            "Projected permission claim type",
            "The normalized projected claim containing candidate Elsa permission strings.",
            "string",
            "text",
            JsonSerializer.SerializeToElement("permissions")),
        Field(
            "allowedPermissions",
            "Allowed permission boundary",
            "Only these exact permission strings may pass through from the projected claim.",
            "string-array",
            "string-array",
            JsonSerializer.SerializeToElement(Array.Empty<string>()))
    ];

    private static SettingFieldDescriptor Field(string name, string displayName, string description, string valueType, string uiHint, JsonElement defaultValue) =>
        new(
            name,
            displayName,
            description,
            valueType,
            true,
            uiHint,
            defaultValue,
            [],
            new SettingFieldValidation(MaximumLength: 16_384),
            false,
            false,
            null,
            null,
            false);
}
