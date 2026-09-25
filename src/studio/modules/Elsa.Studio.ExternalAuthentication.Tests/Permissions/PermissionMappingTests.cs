using System.Text.Json;
using Bunit;
using Elsa.Studio.ExternalAuthentication.Components.PermissionMappings;
using Elsa.Studio.ExternalAuthentication.Components.Preview;
using Elsa.Studio.ExternalAuthentication.Models;
using Elsa.Studio.ExternalAuthentication.Services;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.Permissions;

public sealed class PermissionMappingTests : BunitContext, IAsyncLifetime
{
    public PermissionMappingTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton(TimeProvider.System);
        JSInterop.SetupVoid("mudElementRef.addOnBlurEvent", _ => true).SetVoidResult();
        JSInterop.SetupVoid("mudKeyInterceptor.connect", _ => true).SetVoidResult();
        JSInterop.Setup<int>("mudpopoverHelper.countProviders").SetResult(1);
        Render<MudPopoverProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void ExplicitMappingsExposeUnknownDescriptorsAndDelegationBoundaryViolations()
    {
        var source = CreateMappedSource("workflows:read", "third-party:execute");
        var cut = Render<PermissionMappingEditor>(parameters => parameters
            .Add(component => component.Sources, [source])
            .Add(component => component.Descriptors, [CreateSourceDescriptor()])
            .Add(component => component.PermissionDescriptors, [new PermissionDescriptor { Name = "workflows:read" }])
            .Add(component => component.ActorPermissions, new HashSet<string>(["workflows:read"], StringComparer.Ordinal))
            .Add(component => component.CanDelegate, true));

        Assert.Contains("not advertised by an installed module", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cannot delegate", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Single(cut.Instance.Validate());
    }

    [Fact]
    public void UnrestrictedDelegationStillPreservesUnknownOpenVocabularyWarning()
    {
        var cut = Render<PermissionMappingEditor>(parameters => parameters
            .Add(component => component.Sources, [CreateMappedSource("custom:permission")])
            .Add(component => component.Descriptors, [CreateSourceDescriptor()])
            .Add(component => component.CanDelegate, true)
            .Add(component => component.CanDelegateUnrestricted, true));

        Assert.Empty(cut.Instance.Validate());
        Assert.Contains("remains valid", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InvalidGrantSourceJsonDoesNotReplacePriorSettingsAndBlocksSave()
    {
        var source = CreateMappedSource("workflows:read");
        var original = source.Settings["mappings"].Clone();
        var cut = Render<PermissionMappingEditor>(parameters => parameters
            .Add(component => component.Sources, [source])
            .Add(component => component.Descriptors, [CreateSourceDescriptor()])
            .Add(component => component.ActorPermissions, new HashSet<string>(["workflows:read"], StringComparer.Ordinal))
            .Add(component => component.CanDelegate, true));

        cut.Find("textarea").Input("{broken");

        Assert.Contains("valid JSON", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Single(cut.Instance.Validate());
        Assert.Equal(original.GetRawText(), source.Settings["mappings"].GetRawText());
    }

    [Fact]
    public void PreviewDisplaysPermissionProvenanceAndExplainsThatItHasNoSideEffects()
    {
        var result = new PreviewSignInResult
        {
            Issuer = "https://login.example.test",
            MaskedSubject = "00u…cdef",
            ProposedAction = "Create a credential-less Elsa user",
            PermissionProjection =
            [
                new PermissionProjection
                {
                    Permission = "workflows:read",
                    SourceType = "claim-mapping",
                    SourceReference = "department:engineering"
                }
            ]
        };

        var cut = Render<PermissionPreview>(parameters => parameters
            .Add(component => component.Result, result)
            .Add(component => component.PermissionDescriptors, [new PermissionDescriptor { Name = "workflows:read" }]));

        Assert.Contains("did not create or link a user", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("workflows:read", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("claim-mapping", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("department:engineering", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("not advertised", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    private static PermissionGrantSourceSelection CreateMappedSource(params string[] permissions) => new()
    {
        Type = "claim-mapping",
        SettingsVersion = 1,
        Settings = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            ["claimType"] = JsonSerializer.SerializeToElement("department"),
            ["mappings"] = JsonSerializer.SerializeToElement(new[]
            {
                new { value = "engineering", permissions }
            })
        }
    };

    private static PermissionGrantSourceDescriptor CreateSourceDescriptor() => new()
    {
        Type = "claim-mapping",
        DisplayName = "Claim mapping",
        Description = "Maps projected external claim values to Elsa permissions.",
        SettingsVersion = 1
    };
}
