using System.Net;
using System.Text.Json;
using Bunit;
using Elsa.Api.Client.Resources.Features.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.ExternalAuthentication.Client;
using Elsa.Studio.ExternalAuthentication.Components.ConnectionEditor;
using Elsa.Studio.ExternalAuthentication.Menu;
using Elsa.Studio.ExternalAuthentication.Models;
using ConnectionIndex = Elsa.Studio.ExternalAuthentication.Pages.Connections.Index;
using ConnectionEdit = Elsa.Studio.ExternalAuthentication.Pages.Connections.Edit;
using Elsa.Studio.ExternalAuthentication.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Extensions;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.Connections;

public sealed class ConnectionEditorTests : BunitContext, IAsyncLifetime
{
    private readonly TestConnectionsApi _api = new();
    private readonly TestOperationsApi _operations = new();
    private readonly PermissionService _permissions = new(true);
    private readonly TestCustomEditorRegistry _customEditors = new();
    private readonly IRenderedComponent<MudDialogProvider> _dialogProvider;
    private readonly IRenderedComponent<MudPopoverProvider> _popoverProvider;

    public ConnectionEditorTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton(TimeProvider.System);
        JSInterop.SetupVoid("mudElementRef.addOnBlurEvent", _ => true).SetVoidResult();
        JSInterop.SetupVoid("mudKeyInterceptor.connect", _ => true).SetVoidResult();
        JSInterop.Setup<int>("mudpopoverHelper.countProviders").SetResult(1);
        Services.AddSingleton<IBackendApiClientProvider>(new TestBackendApiClientProvider(_api, _operations));
        Services.AddSingleton<IExternalAuthenticationPermissionService>(_permissions);
        Services.AddSingleton<ICustomConnectionEditorRegistry>(_customEditors);
        _popoverProvider = Render<MudPopoverProvider>();
        _dialogProvider = Render<MudDialogProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();


    [Fact]
    public void ConfigurationOwnedConnection_IsClearlyReadOnly()
    {
        var cut = Render<ConnectionEditor>(parameters => parameters
            .Add(component => component.Connection, CreateConnection(source: "configuration"))
            .Add(component => component.Adapter, CreateAdapter())
            .Add(component => component.Model, CreateMutation())
            .Add(component => component.ReadOnly, true));

        Assert.Contains("configuration-owned and read-only", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("override is unavailable", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Create full Database override", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Save changes", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void GenericEditor_ShowsDerivedCallbackReadOnlyAndDoesNotExposeTenantScope()
    {
        var connection = CreateConnection();
        connection.CallbackUri = "https://elsa.example.test/external-authentication/callback/connection-1";
        connection.PreviewCallbackUri = "https://elsa.example.test/external-authentication/previews/callback/record-1";
        var cut = Render<ConnectionEditor>(parameters => parameters
            .Add(component => component.Connection, connection)
            .Add(component => component.Adapter, CreateAdapter())
            .Add(component => component.Model, CreateMutation()));

        Assert.Contains("Provider callback URI", cut.Markup, StringComparison.Ordinal);
        Assert.Contains(connection.CallbackUri, cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Provider preview callback URI", cut.Markup, StringComparison.Ordinal);
        Assert.Contains(connection.PreviewCallbackUri, cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Tenant ID", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Host-wide", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void GenericEditor_UsesTheOutlinedDenseMudBlazorFieldTreatment()
    {
        var cut = Render<ConnectionEditor>(parameters => parameters
            .Add(component => component.Connection, CreateConnection())
            .Add(component => component.Adapter, CreateAdapter())
            .Add(component => component.Model, CreateMutation()));

        Assert.All(cut.FindComponents<MudTextField<string>>(), field =>
            AssertOutlinedDense(field.Instance.Variant, field.Instance.Margin));
        Assert.All(cut.FindComponents<MudNumericField<int>>(), field =>
            AssertOutlinedDense(field.Instance.Variant, field.Instance.Margin));
        Assert.All(cut.FindComponents<MudSelect<string>>(), field =>
            AssertOutlinedDense(field.Instance.Variant, field.Instance.Margin));
    }

    [Fact]
    public void DescriptorFields_UseTheOutlinedDenseMudBlazorFieldTreatment()
    {
        var settings = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

        var text = Render<DescriptorField>(parameters => parameters
            .Add(component => component.Field, new ConnectionFieldDescriptor
            {
                Name = "description",
                DisplayName = "Description",
                UiHint = "multiline"
            })
            .Add(component => component.Settings, settings));
        var integer = Render<DescriptorField>(parameters => parameters
            .Add(component => component.Field, new ConnectionFieldDescriptor
            {
                Name = "order",
                DisplayName = "Order",
                ValueType = "integer"
            })
            .Add(component => component.Settings, settings));
        var number = Render<DescriptorField>(parameters => parameters
            .Add(component => component.Field, new ConnectionFieldDescriptor
            {
                Name = "weight",
                DisplayName = "Weight",
                ValueType = "number"
            })
            .Add(component => component.Settings, settings));
        var select = Render<DescriptorField>(parameters => parameters
            .Add(component => component.Field, new ConnectionFieldDescriptor
            {
                Name = "mode",
                DisplayName = "Mode",
                AllowedValues = ["discovery", "manual"]
            })
            .Add(component => component.Settings, settings));

        var textField = text.FindComponent<MudTextField<string>>().Instance;
        Assert.Equal(4, textField.Lines);
        AssertOutlinedDense(textField.Variant, textField.Margin);
        var integerField = integer.FindComponent<MudNumericField<int?>>().Instance;
        AssertOutlinedDense(integerField.Variant, integerField.Margin);
        var numberField = number.FindComponent<MudNumericField<decimal?>>().Instance;
        AssertOutlinedDense(numberField.Variant, numberField.Margin);
        var selectField = select.FindComponent<MudSelect<string>>().Instance;
        AssertOutlinedDense(selectField.Variant, selectField.Margin);
    }

    [Fact]
    public void TagsArrayField_AllowsAddingAValueWhenNoOptionsAreProvided()
    {
        var (cut, settings) = RenderTagsArrayField();

        cut.Find("input").Input("email");
        cut.FindAll("button").Single(button => button.TextContent.Contains("Add", StringComparison.Ordinal)).Click();

        Assert.Equal(["email"], settings["scopes"].EnumerateArray().Select(item => item.GetString()));
        Assert.Equal(AlignItems.Start, cut.FindComponents<MudStack>().Single(stack => stack.Instance.Row).Instance.AlignItems);
        Assert.Contains("mt-1", cut.FindComponent<MudButton>().Instance.Class?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? []);
    }

    [Fact]
    public void TagsArrayField_AddsPendingValueWhenEnterIsPressed()
    {
        var (cut, settings) = RenderTagsArrayField();

        cut.Find("input").Input("email");
        cut.Find("input").KeyDown(Key.Enter);

        Assert.Equal(["email"], settings["scopes"].EnumerateArray().Select(item => item.GetString()));
    }

    [Fact]
    public async Task StringArrayField_WithAllowedValuesUsesMultiSelectAndPreservesArrayValues()
    {
        var settings = new Dictionary<string, System.Text.Json.JsonElement>(StringComparer.Ordinal);
        var field = new ConnectionFieldDescriptor
        {
            Name = "scopes",
            DisplayName = "Scopes",
            ValueType = "string-array",
            AllowedValues = ["profile", "email"]
        };
        var cut = Render<DescriptorField>(parameters => parameters
            .Add(component => component.Field, field)
            .Add(component => component.Settings, settings));

        var select = cut.FindComponent<MudSelect<string>>().Instance;
        Assert.True(select.MultiSelection);
        Assert.Equal(["profile", "email"], cut.FindComponents<MudSelectItem<string>>().Select(item => item.Instance.Value));

        await cut.InvokeAsync(() => select.SelectedValuesChanged.InvokeAsync(["profile", "email"]));

        Assert.Empty(DescriptorEditorState.Validate(field, settings));
        Assert.Equal(["profile", "email"], settings["scopes"].EnumerateArray().Select(item => item.GetString()));
    }

    [Fact]
    public void PolicyEditor_UsesRoleOptionsAndFailsClosedWhenTheyAreUnavailable()
    {
        var descriptor = new UnlinkedIdentityPolicyDescriptor
        {
            Type = "create-user",
            DisplayName = "Create user",
            SettingsVersion = 1,
            Fields = [new ConnectionFieldDescriptor { Name = "defaultRoleIds", DisplayName = "Default roles", ValueType = "string-array" }]
        };
        var value = new PolicySelection
        {
            Type = "create-user",
            SettingsVersion = 1,
            Settings = System.Text.Json.JsonSerializer.SerializeToElement(new { defaultRoleIds = Array.Empty<string>() })
        };

        var cut = Render<ConnectionPolicyEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.Descriptors, [descriptor])
            .Add(component => component.Roles, [new IdentityRoleOption { Id = "role-admin", Name = "Administrators" }])
            .Add(component => component.CanSelectRoles, true));

        var roleSelect = cut.FindComponents<MudSelect<string>>().Last().Instance;
        Assert.True(roleSelect.MultiSelection);
        Assert.False(roleSelect.Disabled);
        Assert.All(cut.FindComponents<MudSelect<string>>(), field =>
            AssertOutlinedDense(field.Instance.Variant, field.Instance.Margin));
        Assert.DoesNotContain("Enter role IDs", cut.Markup, StringComparison.OrdinalIgnoreCase);

        cut.Render(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.Descriptors, [descriptor])
            .Add(component => component.RoleOptionsError, "Elsa roles could not be loaded. Default-role settings are read-only."));
        Assert.Contains("read-only", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.True(cut.FindComponents<MudSelect<string>>().Last().Instance.Disabled);
    }

    [Fact]
    public void ConnectionPage_LoadsRoleOptionsWithoutShowingASpuriousError()
    {
        var descriptor = new UnlinkedIdentityPolicyDescriptor
        {
            Type = "create-user",
            DisplayName = "Create user",
            SettingsVersion = 1,
            Fields = [new ConnectionFieldDescriptor { Name = "defaultRoleIds", DisplayName = "Default roles", ValueType = "string-array" }]
        };
        var connection = CreateConnection();
        connection.UnlinkedPolicy = new PolicySelection
        {
            Type = descriptor.Type,
            SettingsVersion = descriptor.SettingsVersion,
            Settings = JsonSerializer.SerializeToElement(new { defaultRoleIds = new[] { "role-admin" } })
        };
        _api.GetResult = connection;
        _api.Adapters = [CreateAdapter()];
        _api.Policies = [descriptor];
        _api.Roles = [new IdentityRoleOption { Id = "role-admin", Name = "Administrators" }];

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, connection.Id));

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("RoleOptionsError", cut.Markup, StringComparison.Ordinal);
            Assert.Contains(
                cut.FindComponent<ConnectionPolicyEditor>().Instance.Roles,
                role => role is { Id: "role-admin", Name: "Administrators" });
            Assert.False(cut.FindComponents<MudSelect<string>>().Last().Instance.Disabled);
        });
    }

    [Fact]
    public void ConnectionPage_ShowsTheActualManagedSecretResolverError()
    {
        var connection = CreateConnection();
        _api.GetResult = connection;
        _api.Adapters = [CreateAdapter(includeSecret: true)];
        _api.ManagedSecretResolvers = [];

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, connection.Id));
        cut.WaitForAssertion(() => Assert.Equal(
            "Managed secret storage is not available on this Elsa server.",
            cut.FindComponents<ConnectionEditor>()
                .Single(component => component.Instance.Section == ConnectionEditorSection.Provider)
                .Instance.ManagedSecretResolverError));
        cut.FindAll(".mud-tab").Single(tab => tab.TextContent.Contains("Provider", StringComparison.Ordinal)).Click();

        cut.WaitForAssertion(() =>
        {
            var field = cut.FindComponent<SecretBindingField>().Instance;
            Assert.False(field.ReadOnly);
            Assert.True(field.CanManage);
            Assert.Equal("Managed secret storage is not available on this Elsa server.", field.ManagedSecretResolverError);
            Assert.DoesNotContain("ManagedSecretResolverError", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ConnectionPage_SeparatesManagementTasksIntoWorkspaceTabs()
    {
        var connection = CreateConnection();
        connection.EnabledIntent = true;
        connection.EffectivelyEnabled = true;
        connection.Validity = "valid";
        _api.GetResult = connection;
        _api.Adapters = [CreateAdapter()];

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, connection.Id));

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(
                ["General", "Provider", "Provisioning", "Diagnostics"],
                cut.FindComponents<MudTabPanel>().Select(panel => panel.Instance.Text));
            var tabs = cut.FindComponent<MudTabs>().Instance;
            Assert.True(tabs.KeepPanelsAlive);
            Assert.Equal("pa-4 pa-sm-6", tabs.TabPanelsClass);
            Assert.Equal(0, tabs.GetState(component => component.ActivePanelIndex));
            var tabPanels = cut.Find(".mud-tabs-panels");
            Assert.Contains("pa-4", tabPanels.ClassList);
            Assert.Contains("pa-sm-6", tabPanels.ClassList);
            Assert.NotNull(tabPanels.QuerySelector(".connection-workspace__general"));
            Assert.NotNull(tabPanels.QuerySelector(".connection-workspace__provider"));
            Assert.NotNull(tabPanels.QuerySelector(".connection-workspace__provisioning"));
            Assert.NotNull(tabPanels.QuerySelector(".connection-workspace__diagnostics"));
            var header = cut.Find(".connection-workspace__header");
            var general = cut.Find(".connection-workspace__general");
            var provider = cut.Find(".connection-workspace__provider");
            Assert.Contains("Effective: Database", header.TextContent, StringComparison.Ordinal);
            Assert.Contains("Display name", general.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("OpenID Connect settings", general.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("Secret bindings", general.TextContent, StringComparison.Ordinal);
            Assert.Contains("OpenID Connect settings", provider.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("At a glance", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Effective source", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Record source", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Open Diagnostics", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("User provisioning and linking", cut.Find(".connection-workspace__provisioning").TextContent, StringComparison.Ordinal);
            Assert.Contains("Provider connectivity", cut.Find(".connection-workspace__diagnostics").TextContent, StringComparison.Ordinal);
            Assert.Contains("Available for sign-in", cut.Markup, StringComparison.Ordinal);
            var actions = cut.Find(".connection-workspace__actions");
            Assert.Contains("pa-4", actions.ClassList);
            Assert.Contains("pa-sm-6", actions.ClassList);
            var actionButtons = actions.QuerySelectorAll("button");
            Assert.Equal(["Cancel", "Save changes"], actionButtons.Select(button => button.TextContent.Trim()));
            Assert.All(actionButtons, button =>
            {
                Assert.Contains("mud-button-text", button.ClassList);
                Assert.DoesNotContain("mud-button-outlined", button.ClassList);
                Assert.DoesNotContain("mud-button-filled", button.ClassList);
            });
            var saveButton = actionButtons.Single(button => button.TextContent.Contains("Save changes", StringComparison.Ordinal));
            Assert.Contains("mud-button-text-primary", saveButton.ClassList);
            Assert.True(saveButton.HasAttribute("disabled"));
        });

        cut.FindAll(".mud-tab").Single(tab => tab.TextContent.Contains("Provisioning", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, cut.FindComponent<MudTabs>().Instance.GetState(component => component.ActivePanelIndex));
            Assert.Equal(1, cut.Find(".connection-workspace__provisioning").TextContent.Split("User provisioning and linking", StringSplitOptions.None).Length - 1);
        });

        cut.FindAll(".mud-tab").Single(tab => tab.TextContent.Contains("Diagnostics", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindComponent<MudTabs>().Instance.GetState(component => component.ActivePanelIndex)));
    }

    [Fact]
    public void DisabledConnection_SaveAndActivateUpdatesChecksAndActivatesTheRevision()
    {
        var initial = CreateConnection();
        initial.AdapterSettings["authority"] = JsonSerializer.SerializeToElement("https://login.example.test");
        var updated = CreateConnection();
        updated.Revision = initial.Revision + 1;
        updated.Validity = "unknown";
        var validated = CreateConnection();
        validated.Revision = updated.Revision;
        validated.Validity = "valid";
        var enabled = CreateConnection();
        enabled.Revision = updated.Revision + 1;
        enabled.Validity = "valid";
        enabled.EnabledIntent = true;
        enabled.EffectivelyEnabled = true;
        _api.GetResults.Enqueue(initial);
        _api.GetResults.Enqueue(validated);
        _api.GetResults.Enqueue(enabled);
        _api.GetResult = updated;
        _api.Adapters = [CreateAdapter()];

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, initial.Id));

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(MaxWidth.ExtraLarge, cut.FindComponent<MudContainer>().Instance.MaxWidth);
            var actions = cut.Find(".connection-workspace__actions");
            Assert.Equal(2, actions.QuerySelectorAll("button").Length);
            Assert.False(actions.QuerySelectorAll("button")
                .Single(button => button.TextContent.Contains("Check and activate", StringComparison.Ordinal))
                .HasAttribute("disabled"));
        });

        var displayNameLabel = cut.FindAll("label").Single(element => element.TextContent.Contains("Display name", StringComparison.Ordinal));
        cut.Find($"#{displayNameLabel.GetAttribute("for")}").Change("Updated connection");

        cut.WaitForAssertion(() => Assert.Contains("Save and activate", cut.Find(".connection-workspace__actions").TextContent, StringComparison.Ordinal));
        cut.Find(".connection-workspace__actions")
            .QuerySelectorAll("button")
            .Single(button => button.TextContent.Contains("Save and activate", StringComparison.Ordinal))
            .Click();

        cut.WaitForAssertion(() => Assert.Equal(initial.Id, _api.EnabledConnectionId));
        Assert.Equal("Updated connection", _api.UpdatedRequest!.DisplayName);
        Assert.Equal(initial.Id, _api.ValidatedConnectionId);
        Assert.Equal(ConnectionConcurrency.ToIfMatch(validated.Revision), _api.EnabledIfMatch);
    }

    [Fact]
    public void ValidDisabledConnection_ActivatesWithoutRepeatingValidation()
    {
        var valid = CreateConnection();
        valid.Validity = "valid";
        var enabled = CreateConnection();
        enabled.Validity = "valid";
        enabled.EnabledIntent = true;
        enabled.EffectivelyEnabled = true;
        _api.GetResults.Enqueue(valid);
        _api.GetResults.Enqueue(enabled);
        _api.Adapters = [CreateAdapter()];

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, valid.Id));
        cut.WaitForAssertion(() => Assert.Contains("Activate", cut.Find(".connection-workspace__actions").TextContent, StringComparison.Ordinal));
        cut.Find(".connection-workspace__actions")
            .QuerySelectorAll("button")
            .Single(button => button.TextContent.Trim() == "Activate")
            .Click();

        cut.WaitForAssertion(() => Assert.Equal(valid.Id, _api.EnabledConnectionId));
        Assert.Null(_api.ValidatedConnectionId);
        Assert.Equal(ConnectionConcurrency.ToIfMatch(valid.Revision), _api.EnabledIfMatch);
    }

    [Fact]
    public void ValidationWarningsDoNotAddAnotherActivationPrompt()
    {
        var initial = CreateConnection();
        initial.Validity = "unknown";
        var validated = CreateConnection();
        validated.Validity = "valid";
        validated.ValidationWarnings = ["Provider metadata omitted an optional capability."];
        var enabled = CreateConnection();
        enabled.Validity = "valid";
        enabled.EnabledIntent = true;
        enabled.EffectivelyEnabled = true;
        enabled.ValidationWarnings = validated.ValidationWarnings;
        _api.GetResults.Enqueue(initial);
        _api.GetResults.Enqueue(validated);
        _api.GetResults.Enqueue(enabled);
        _api.Adapters = [CreateAdapter()];
        _api.ValidationResult = new ConnectionValidationResult
        {
            Valid = true,
            Warnings = validated.ValidationWarnings
        };

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, initial.Id));
        cut.WaitForAssertion(() => Assert.Contains("Check and activate", cut.Find(".connection-workspace__actions").TextContent, StringComparison.Ordinal));
        cut.Find(".connection-workspace__actions")
            .QuerySelectorAll("button")
            .Single(button => button.TextContent.Contains("Check and activate", StringComparison.Ordinal))
            .Click();

        cut.WaitForAssertion(() => Assert.Equal(initial.Id, _api.EnabledConnectionId));
        Assert.DoesNotContain("Activate", _dialogProvider.Markup, StringComparison.Ordinal);
        Assert.Contains("Provider metadata omitted an optional capability.", cut.Find(".connection-workspace__diagnostics").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void NewConnection_KeepsDraftSecretGuidanceInsideTheProviderTask()
    {
        _api.Adapters = [CreateAdapter(includeSecret: true)];

        var cut = Render<ConnectionEdit>();

        cut.WaitForAssertion(() =>
        {
            var provider = cut.Find(".connection-workspace__provider");
            Assert.Equal(1, provider.TextContent.Split("Save the connection draft before configuring secrets.", StringSplitOptions.None).Length - 1);
            Assert.Empty(cut.FindComponents<SecretBindingField>());
            Assert.DoesNotContain("Secret bindings", cut.Find(".connection-workspace__general").TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ConnectionKey_ExplainsItsUsageAndImmutability()
    {
        _api.Adapters = [CreateAdapter()];

        var cut = Render<ConnectionEdit>();

        Assert.Contains("Stable identifier used in sign-in URLs and configuration. Suggested from the display name and cannot be changed after creation.", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void NewConnection_SuggestsAKeyUntilTheUserOverridesIt()
    {
        _api.Adapters = [CreateAdapter()];

        var cut = Render<ConnectionEdit>();
        ChangeField(cut, "Display name", "Keycloak Workforce");
        cut.WaitForAssertion(() => Assert.Equal("keycloak-workforce", FieldValue(cut, "Connection key")));

        ChangeField(cut, "Display name", "Contoso Workforce");
        cut.WaitForAssertion(() => Assert.Equal("contoso-workforce", FieldValue(cut, "Connection key")));

        ChangeField(cut, "Connection key", "workforce-sso");
        ChangeField(cut, "Display name", "Renamed Workforce");

        cut.WaitForAssertion(() => Assert.Equal("workforce-sso", FieldValue(cut, "Connection key")));

        ChangeField(cut, "Connection key", string.Empty);
        ChangeField(cut, "Display name", "Another Workforce");

        cut.WaitForAssertion(() => Assert.Equal(string.Empty, FieldValue(cut, "Connection key")));
    }

    [Fact]
    public void NewConnection_CanSaveIncompleteDraftBeforeConfiguringRequiredSecretBindings()
    {
        _api.Adapters = [CreateAdapter(includeSecret: true)];

        var cut = Render<ConnectionEdit>();
        ChangeField(cut, "Display name", "Contoso");
        ChangeField(cut, "Connection key", "contoso");

        cut.WaitForAssertion(() =>
        {
            var actions = cut.Find(".connection-workspace__actions").QuerySelectorAll("button");
            Assert.False(actions.Single(button => button.TextContent.Contains("Save as draft", StringComparison.Ordinal)).HasAttribute("disabled"));
            Assert.True(actions.Single(button => button.TextContent.Contains("Save and activate", StringComparison.Ordinal)).HasAttribute("disabled"));
        });

        cut.Find(".connection-workspace__actions")
            .QuerySelectorAll("button")
            .Single(button => button.TextContent.Contains("Save as draft", StringComparison.Ordinal))
            .Click();

        cut.WaitForAssertion(() => Assert.NotNull(_api.CreatedRequest));
        Assert.Null(_api.ValidatedConnectionId);
        Assert.Null(_api.EnabledConnectionId);
    }

    [Fact]
    public void ConnectionWorkspace_EnablesSaveOnlyWhenDirtyAndStructurallyValid()
    {
        _api.Adapters = [CreateAdapter()];

        var cut = Render<ConnectionEdit>();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".connection-workspace__actions")));

        ChangeField("Display name", "Contoso");
        ChangeField("Connection key", "contoso");
        Assert.True(SaveButton().HasAttribute("disabled"));

        ChangeField("Authority", "https://login.example.test");
        cut.WaitForAssertion(() => Assert.False(SaveButton().HasAttribute("disabled")));

        void ChangeField(string label, string value)
        {
            var fieldLabel = cut.FindAll("label").Single(element => element.TextContent.Contains(label, StringComparison.Ordinal));
            cut.Find($"#{fieldLabel.GetAttribute("for")}").Change(value);
        }

        AngleSharp.Dom.IElement SaveButton() => cut.Find(".connection-workspace__actions")
            .QuerySelectorAll("button")
            .Single(button => button.TextContent.Contains("Save and activate", StringComparison.Ordinal));
    }

    [Fact]
    public void NewConnection_SaveAndActivateMakesTheValidatedRevisionAvailable()
    {
        var created = CreateConnection();
        created.Validity = "unknown";
        var validated = CreateConnection();
        validated.Validity = "valid";
        _api.Adapters = [CreateAdapter()];
        _api.CreateResult = created;
        var enabled = CreateConnection();
        enabled.EnabledIntent = true;
        enabled.EffectivelyEnabled = true;
        _api.GetResults.Enqueue(validated);
        _api.GetResults.Enqueue(enabled);

        var cut = Render<ConnectionEdit>();
        cut.WaitForAssertion(() => Assert.Contains("Save and activate", cut.Find(".connection-workspace__actions").TextContent, StringComparison.Ordinal));

        ChangeField(cut, "Display name", "Contoso");
        ChangeField(cut, "Connection key", "contoso");
        ChangeField(cut, "Authority", "https://login.example.test");
        cut.Find(".connection-workspace__actions")
            .QuerySelectorAll("button")
            .Single(button => button.TextContent.Contains("Save and activate", StringComparison.Ordinal))
            .Click();

        cut.WaitForAssertion(() => Assert.Equal(created.Id, _api.EnabledConnectionId));
        Assert.NotNull(_api.CreatedRequest);
        Assert.Equal(created.Id, _api.ValidatedConnectionId);
        Assert.Equal(ConnectionConcurrency.ToIfMatch(validated.Revision), _api.EnabledIfMatch);

    }

    [Fact]
    public void NewConnection_SaveAsDraftDoesNotValidateOrEnable()
    {
        _api.Adapters = [CreateAdapter()];

        var cut = Render<ConnectionEdit>();
        ChangeField(cut, "Display name", "Contoso");
        ChangeField(cut, "Connection key", "contoso");
        ChangeField(cut, "Authority", "https://login.example.test");
        cut.Find(".connection-workspace__actions")
            .QuerySelectorAll("button")
            .Single(button => button.TextContent.Contains("Save as draft", StringComparison.Ordinal))
            .Click();

        cut.WaitForAssertion(() => Assert.NotNull(_api.CreatedRequest));
        Assert.Null(_api.ValidatedConnectionId);
        Assert.Null(_api.EnabledConnectionId);

    }

    [Fact]
    public void CreateOnlyUserCanSaveADraftButCannotActivateIt()
    {
        _permissions.AllowedPermissions = new HashSet<string>([ExternalAuthenticationPermissions.Create], StringComparer.Ordinal);
        _api.Adapters = [CreateAdapter()];

        var cut = Render<ConnectionEdit>();
        ChangeField(cut, "Display name", "Contoso");
        ChangeField(cut, "Connection key", "contoso");
        ChangeField(cut, "Authority", "https://login.example.test");

        cut.WaitForAssertion(() =>
        {
            var actions = cut.Find(".connection-workspace__actions").QuerySelectorAll("button");
            Assert.False(actions.Single(button => button.TextContent.Contains("Save as draft", StringComparison.Ordinal)).HasAttribute("disabled"));
            Assert.True(actions.Single(button => button.TextContent.Contains("Save and activate", StringComparison.Ordinal)).HasAttribute("disabled"));
        });

    }

    [Fact]
    public async Task NewConnection_InvalidActivationKeepsARecoverableDraftAndOpensDiagnostics()
    {
        var created = CreateConnection();
        created.Validity = "unknown";
        var invalid = CreateConnection();
        invalid.Validity = "invalid";
        invalid.ValidationErrors =
        [
            new ConnectionValidationMessage
            {
                Field = "adapterSettings.authority",
                Code = "invalid",
                Message = "Authority could not be resolved."
            }
        ];
        _api.Adapters = [CreateAdapter()];
        _api.CreateResult = created;
        _api.GetResults.Enqueue(invalid);
        _api.GetResult = invalid;
        _api.ValidationResult = new ConnectionValidationResult { Valid = false, Errors = invalid.ValidationErrors };

        var cut = Render<ConnectionEdit>();
        ChangeField(cut, "Display name", "Contoso");
        ChangeField(cut, "Connection key", "contoso");
        ChangeField(cut, "Authority", "https://login.example.test");
        await cut.InvokeAsync(() => cut.FindAll("button").Single(button => button.TextContent.Contains("Save and activate", StringComparison.Ordinal)).Click());

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(3, cut.FindComponent<MudTabs>().Instance.GetState(component => component.ActivePanelIndex));
            Assert.Contains("Authority could not be resolved.", cut.Find(".connection-workspace__diagnostics").TextContent, StringComparison.Ordinal);
        });
        Assert.Null(_api.EnabledConnectionId);
        Assert.EndsWith($"/security/external-authentication/connections/{created.Id}", Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal);

    }

    [Fact]
    public async Task NewConnection_EnableFailureKeepsTheValidRevisionAvailableForDirectRetry()
    {
        var created = CreateConnection();
        created.Validity = "unknown";
        var validated = CreateConnection();
        validated.Validity = "valid";
        var enabled = CreateConnection();
        enabled.Validity = "valid";
        enabled.EnabledIntent = true;
        enabled.EffectivelyEnabled = true;
        _api.Adapters = [CreateAdapter()];
        _api.CreateResult = created;
        _api.GetResults.Enqueue(validated);
        _api.GetResult = validated;
        _api.EnableException = new InvalidOperationException("Enable failed.");

        var cut = Render<ConnectionEdit>();
        ChangeField(cut, "Display name", "Contoso");
        ChangeField(cut, "Connection key", "contoso");
        ChangeField(cut, "Authority", "https://login.example.test");
        await cut.InvokeAsync(() => cut.FindAll("button").Single(button => button.TextContent.Contains("Save and activate", StringComparison.Ordinal)).Click());

        cut.WaitForAssertion(() => Assert.Contains("Activate", cut.Find(".connection-workspace__actions").TextContent, StringComparison.Ordinal));
        Assert.Equal(1, _api.ValidationRequests);
        Assert.Equal(1, _api.EnableRequests);

        _api.EnableException = null;
        _api.GetResults.Enqueue(enabled);
        await cut.InvokeAsync(() => cut.Find(".connection-workspace__actions")
            .QuerySelectorAll("button")
            .Single(button => button.TextContent.Trim() == "Activate")
            .Click());

        cut.WaitForAssertion(() => Assert.Equal(2, _api.EnableRequests));
        Assert.Equal(1, _api.ValidationRequests);

    }

    [Fact]
    public async Task ActivationDisablesNavigationAndDuplicateSubmissionWhileChecksAreRunning()
    {
        var created = CreateConnection();
        created.Validity = "unknown";
        var validated = CreateConnection();
        validated.Validity = "valid";
        var enabled = CreateConnection();
        enabled.Validity = "valid";
        enabled.EnabledIntent = true;
        enabled.EffectivelyEnabled = true;
        var pendingValidation = new TaskCompletionSource<ConnectionValidationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _api.Adapters = [CreateAdapter()];
        _api.CreateResult = created;
        _api.PendingValidation = pendingValidation;
        _api.GetResults.Enqueue(validated);
        _api.GetResults.Enqueue(enabled);

        var cut = Render<ConnectionEdit>();
        ChangeField(cut, "Display name", "Contoso");
        ChangeField(cut, "Connection key", "contoso");
        ChangeField(cut, "Authority", "https://login.example.test");
        var activation = cut.InvokeAsync(() => cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Save and activate", StringComparison.Ordinal))
            .Click());

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Checking configuration", cut.Find(".connection-workspace__actions").TextContent, StringComparison.Ordinal);
            Assert.All(cut.Find(".connection-workspace__actions").QuerySelectorAll("button"), button => Assert.True(button.HasAttribute("disabled")));
            Assert.True(cut.FindComponent<Microsoft.AspNetCore.Components.Routing.NavigationLock>().Instance.ConfirmExternalNavigation);
        });

        pendingValidation.SetResult(new ConnectionValidationResult { Valid = true });
        await activation;
        Assert.Equal(1, _api.CreateRequests);
        Assert.Equal(1, _api.ValidationRequests);
        cut.WaitForAssertion(() => Assert.Equal(1, _api.EnableRequests));
    }

    [Fact]
    public void ActivationDoesNotReportSuccessWhenTheAuthoritativeConnectionIsNotEffective()
    {
        var valid = CreateConnection();
        valid.Validity = "valid";
        var ineffective = CreateConnection();
        ineffective.Validity = "valid";
        ineffective.EnabledIntent = true;
        ineffective.EffectivelyEnabled = false;
        _api.GetResults.Enqueue(valid);
        _api.GetResults.Enqueue(ineffective);
        _api.Adapters = [CreateAdapter()];

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, valid.Id));
        cut.WaitForAssertion(() => Assert.Contains("Activate", cut.Find(".connection-workspace__actions").TextContent, StringComparison.Ordinal));
        cut.Find(".connection-workspace__actions").QuerySelectorAll("button").Single(button => button.TextContent.Trim() == "Activate").Click();

        cut.WaitForAssertion(() => Assert.Single(Services.GetRequiredService<ISnackbar>().ShownSnackbars));
        var snackbar = Assert.Single(Services.GetRequiredService<ISnackbar>().ShownSnackbars);
        Assert.DoesNotContain("available for sign-in.", snackbar.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not available for sign-in", snackbar.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Retry activation", cut.Find(".connection-workspace__actions").TextContent, StringComparison.Ordinal);

        var enabled = CreateConnection();
        enabled.Validity = "valid";
        enabled.EnabledIntent = true;
        enabled.EffectivelyEnabled = true;
        _api.GetResults.Enqueue(enabled);
        cut.Find(".connection-workspace__actions").QuerySelectorAll("button").Single(button => button.TextContent.Contains("Retry activation", StringComparison.Ordinal)).Click();

        cut.WaitForAssertion(() => Assert.Equal(2, _api.EnableRequests));
        Assert.Equal(0, _api.ValidationRequests);
    }

    [Fact]
    public void ActivationOfAChangedRevisionNeverCallsEnable()
    {
        var initial = CreateConnection();
        initial.Validity = "unknown";
        var changed = CreateConnection();
        changed.Validity = "valid";
        changed.Revision = initial.Revision + 1;
        _api.GetResults.Enqueue(initial);
        _api.GetResults.Enqueue(changed);
        _api.Adapters = [CreateAdapter()];

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, initial.Id));
        cut.WaitForAssertion(() => Assert.Contains("Check and activate", cut.Find(".connection-workspace__actions").TextContent, StringComparison.Ordinal));
        cut.Find(".connection-workspace__actions").QuerySelectorAll("button").Single(button => button.TextContent.Contains("Check and activate", StringComparison.Ordinal)).Click();

        cut.WaitForAssertion(() => Assert.Single(Services.GetRequiredService<ISnackbar>().ShownSnackbars));
        Assert.Equal(0, _api.EnableRequests);
        Assert.Contains("changed while it was being checked", Assert.Single(Services.GetRequiredService<ISnackbar>().ShownSnackbars).Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PostEnableRefreshFailureNeverReportsActivationSuccess()
    {
        var valid = CreateConnection();
        valid.Validity = "valid";
        _api.GetResult = valid;
        _api.GetExceptionAfterRequest = 2;
        _api.GetException = new HttpRequestException("Refresh failed.");
        _api.Adapters = [CreateAdapter()];

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, valid.Id));
        cut.WaitForAssertion(() => Assert.Contains("Activate", cut.Find(".connection-workspace__actions").TextContent, StringComparison.Ordinal));
        cut.Find(".connection-workspace__actions").QuerySelectorAll("button").Single(button => button.TextContent.Trim() == "Activate").Click();

        cut.WaitForAssertion(() => Assert.Single(Services.GetRequiredService<ISnackbar>().ShownSnackbars));
        var snackbar = Assert.Single(Services.GetRequiredService<ISnackbar>().ShownSnackbars);
        Assert.Contains("could not confirm", snackbar.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Connection is available", snackbar.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(cut.Find(".connection-workspace__actions").QuerySelectorAll("button").Single(button => button.TextContent.Trim() == "Activate").HasAttribute("disabled"));
    }

    [Fact]
    public void AmbiguousEnableFailureReconcilesAuthoritativeSuccess()
    {
        var valid = CreateConnection();
        valid.Validity = "valid";
        var enabled = CreateConnection();
        enabled.Validity = "valid";
        enabled.EnabledIntent = true;
        enabled.EffectivelyEnabled = true;
        _api.GetResults.Enqueue(valid);
        _api.GetResults.Enqueue(enabled);
        _api.EnableException = new HttpRequestException("The response was lost.");
        _api.Adapters = [CreateAdapter()];

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, valid.Id));
        cut.WaitForAssertion(() => Assert.Contains("Activate", cut.Find(".connection-workspace__actions").TextContent, StringComparison.Ordinal));
        cut.Find(".connection-workspace__actions").QuerySelectorAll("button").Single(button => button.TextContent.Trim() == "Activate").Click();

        cut.WaitForAssertion(() => Assert.Single(Services.GetRequiredService<ISnackbar>().ShownSnackbars));
        Assert.Contains("available for sign-in", Assert.Single(Services.GetRequiredService<ISnackbar>().ShownSnackbars).Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NewConnection_UsesDraftWorkspaceWithoutDiagnostics()
    {
        _api.Adapters = [CreateAdapter()];

        var cut = Render<ConnectionEdit>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(
                ["General", "Provider", "Provisioning"],
                cut.FindComponents<MudTabPanel>().Select(panel => panel.Instance.Text));
            var header = cut.Find(".connection-workspace__header");
            Assert.Contains("Create identity provider connection", header.TextContent, StringComparison.Ordinal);
            Assert.Contains("Draft", header.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("Validate", header.TextContent, StringComparison.Ordinal);
            Assert.Contains("Provider protocol", cut.Find(".connection-workspace__general").TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("Diagnostics", cut.Markup, StringComparison.Ordinal);
            Assert.NotNull(cut.Find(".connection-workspace__actions"));
            var providerProtocol = cut.FindComponents<MudSelect<string>>()
                .Single(field => string.Equals(field.Instance.Label, "Provider protocol", StringComparison.Ordinal));
            AssertOutlinedDense(providerProtocol.Instance.Variant, providerProtocol.Instance.Margin);
        });
    }

    [Fact]
    public void NewConnection_InitializesDocumentedProviderDefaultsAndKeepsDiscoverySettingsVisible()
    {
        var adapter = new AdapterDescriptor
        {
            Type = "openid-connect",
            DisplayName = "OpenID Connect",
            Fields =
            [
                new ConnectionFieldDescriptor
                {
                    Name = "clientAuthenticationMethod",
                    DisplayName = "Client authentication",
                    AllowedValues = ["client_secret_basic", "client_secret_post"]
                },
                new ConnectionFieldDescriptor
                {
                    Name = "mode",
                    DisplayName = "Trust mode",
                    AllowedValues = ["discovery", "manual"]
                },
                new ConnectionFieldDescriptor
                {
                    Name = "discoveryUrl",
                    DisplayName = "Discovery URL",
                    IsRequired = true,
                    VisibleWhen = new ConnectionFieldVisibilityCondition { Field = "mode", ExpectedValue = "discovery" }
                }
            ]
        };
        _api.Adapters = [adapter];

        var cut = Render<ConnectionEdit>();

        cut.WaitForAssertion(() =>
        {
            var editor = cut.FindComponents<ConnectionEditor>()
                .Single(component => component.Instance.Section == ConnectionEditorSection.Provider)
                .Instance;
            Assert.Equal("client_secret_basic", DescriptorEditorState.ToDisplayString(editor.Model.AdapterSettings["clientAuthenticationMethod"]));
            Assert.Equal("discovery", DescriptorEditorState.ToDisplayString(editor.Model.AdapterSettings["mode"]));
            Assert.False(cut.FindComponent<Microsoft.AspNetCore.Components.Routing.NavigationLock>().Instance.ConfirmExternalNavigation);
            Assert.Contains("Discovery URL", cut.Find(".connection-workspace__provider").TextContent, StringComparison.Ordinal);
            Assert.Contains("Client secret (basic authentication)", cut.Find(".connection-workspace__provider").TextContent, StringComparison.Ordinal);
            Assert.Contains("Discovery", cut.Find(".connection-workspace__provider").TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ConnectionEditor_PresentsFixedPkceAsStaticS256InsteadOfAnEmptyInput()
    {
        var adapter = new AdapterDescriptor
        {
            Type = "openid-connect",
            DisplayName = "OpenID Connect",
            Fields = [new ConnectionFieldDescriptor { Name = "providerPkce", DisplayName = "Provider PKCE", IsReadOnly = true }]
        };

        var cut = Render<ConnectionEditor>(parameters => parameters
            .Add(component => component.Connection, CreateConnection())
            .Add(component => component.Adapter, adapter)
            .Add(component => component.Model, CreateMutation()));

        var pkce = cut.Find(".connection-editor__fixed-pkce");
        Assert.Contains("Provider PKCE", pkce.TextContent, StringComparison.Ordinal);
        Assert.Contains("S256", pkce.TextContent, StringComparison.Ordinal);
        Assert.Empty(pkce.QuerySelectorAll("input"));
    }

    [Fact]
    public void ConnectionEditor_ReportsEditsForUnsavedChangeProtection()
    {
        var changes = 0;
        var cut = Render<ConnectionEditor>(parameters => parameters
            .Add(component => component.Connection, CreateConnection())
            .Add(component => component.Adapter, CreateAdapter())
            .Add(component => component.Model, CreateMutation())
            .Add(component => component.Changed, EventCallback.Factory.Create(this, () => changes++)));

        cut.FindAll("input").First().Change("Updated connection");

        Assert.Equal(1, changes);
        Assert.Equal("Updated connection", cut.Instance.Model.DisplayName);
    }

    [Fact]
    public async Task ConnectionEditor_ReportsUnsafeTrustConfirmationForUnsavedChangeProtection()
    {
        var changes = 0;
        var mutation = CreateMutation();
        mutation.AdapterSettings["allowInsecureMetadata"] = System.Text.Json.JsonSerializer.SerializeToElement(true);
        var cut = Render<ConnectionEditor>(parameters => parameters
            .Add(component => component.Connection, CreateConnection())
            .Add(component => component.Adapter, CreateAdapter(includeUnsafe: true))
            .Add(component => component.Model, mutation)
            .Add(component => component.CanConfigureUnsafeSettings, true)
            .Add(component => component.Changed, EventCallback.Factory.Create(this, () => changes++)));

        var confirmation = cut.FindComponents<MudCheckBox<bool>>()
            .Single(component => component.Instance.Label?.Contains("explicitly confirm", StringComparison.Ordinal) == true);
        await cut.InvokeAsync(() => confirmation.Instance.ValueChanged.InvokeAsync(true));

        Assert.True(mutation.ConfirmUnsafeSettings);
        Assert.Equal(1, changes);
    }

    [Fact]
    public async Task SavedConnection_AllowsManagedSecretConfiguration()
    {
        var connection = CreateConnection();
        _api.GetResult = connection;
        _api.Adapters = [CreateAdapter(includeSecret: true)];
        _api.ManagedSecretResolvers = [new ManagedSecretResolverDescriptor { Type = "elsa-secrets", DisplayName = "Elsa Secrets" }];

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, connection.Id));
        await cut.InvokeAsync(() => cut.FindComponent<MudTabs>().Instance.ActivatePanelAsync(1));

        cut.WaitForAssertion(() =>
        {
            var secrets = cut.Find(".connection-editor__secrets");
            Assert.Contains("Replace managed secret", secrets.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("Save the connection draft", secrets.TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ConfigurationOwnedConnection_UsesReadOnlyWorkspaceContext()
    {
        var connection = CreateConnection("configuration");
        _api.GetResult = connection;
        _api.Adapters = [CreateAdapter()];

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, connection.Id));

        cut.WaitForAssertion(() =>
        {
            var header = cut.Find(".connection-workspace__header");
            Assert.Contains("Effective: Deployment", header.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("Validate", header.TextContent, StringComparison.Ordinal);
            Assert.Contains("deployment-owned connection is read-only", cut.Find(".connection-workspace__ownership").TextContent, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(cut.FindAll(".connection-workspace__actions"));
            Assert.DoesNotContain("Save changes", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("At a glance", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Open Diagnostics", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Provider connectivity", cut.Find(".connection-workspace__diagnostics").TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("Lifecycle", cut.Find(".connection-workspace__diagnostics").TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void RecheckFromDiagnosticsOpensDiagnosticsWithTheResult()
    {
        var connection = CreateConnection();
        _api.GetResult = connection;
        _api.Adapters = [CreateAdapter()];
        _api.ValidationResult = new ConnectionValidationResult
        {
            Valid = false,
            Errors =
            [
                new ConnectionValidationMessage
                {
                    Field = "adapterSettings.authority",
                    Code = "invalid",
                    Message = "Authority could not be resolved."
                }
            ]
        };
        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, connection.Id));
        cut.WaitForAssertion(() => Assert.Contains("Re-check configuration", cut.Find(".connection-workspace__diagnostics").TextContent, StringComparison.Ordinal));
        RecheckConfiguration(cut);

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(3, cut.FindComponent<MudTabs>().Instance.GetState(component => component.ActivePanelIndex));
            Assert.Contains("Authority could not be resolved.", cut.Find(".connection-workspace__diagnostics").TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void InvalidConnection_ShowsAuthoritativeReasonsInDiagnosticsWithoutRevalidation()
    {
        var connection = CreateConnection();
        connection.Validity = "invalid";
        connection.ValidationErrors =
        [
            new ConnectionValidationMessage
            {
                Field = "adapterSettings.authority",
                Code = "invalid",
                Message = "Authority could not be resolved."
            }
        ];
        connection.ValidationWarnings = ["Provider metadata omitted an optional capability."];
        _api.GetResult = connection;
        _api.Adapters = [CreateAdapter()];

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, connection.Id));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Needs attention", cut.Find(".connection-workspace__header").TextContent, StringComparison.Ordinal);
            var diagnostics = cut.Find(".connection-workspace__diagnostics").TextContent;
            Assert.Contains("The current revision is invalid.", diagnostics, StringComparison.Ordinal);
            Assert.Contains("Authority could not be resolved.", diagnostics, StringComparison.Ordinal);
            Assert.Contains("Provider metadata omitted an optional capability.", diagnostics, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ValidationResponseCannotContradictAuthoritativeRevisionStatus()
    {
        var initial = CreateConnection();
        initial.Validity = "unknown";
        var invalid = CreateConnection();
        invalid.Validity = "invalid";
        invalid.ValidationErrors =
        [
            new ConnectionValidationMessage
            {
                Field = "adapterSettings.authority",
                Code = "invalid",
                Message = "Authority could not be resolved."
            }
        ];
        _api.GetResults.Enqueue(initial);
        _api.GetResults.Enqueue(invalid);
        _api.Adapters = [CreateAdapter()];
        _api.ValidationResult = new ConnectionValidationResult { Valid = true };

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, initial.Id));
        cut.WaitForAssertion(() => Assert.Contains("Re-check configuration", cut.Find(".connection-workspace__diagnostics").TextContent, StringComparison.Ordinal));
        RecheckConfiguration(cut);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Needs attention", cut.Find(".connection-workspace__header").TextContent, StringComparison.Ordinal);
            var diagnostics = cut.Find(".connection-workspace__diagnostics").TextContent;
            Assert.Contains("The current revision is invalid.", diagnostics, StringComparison.Ordinal);
            Assert.Contains("Authority could not be resolved.", diagnostics, StringComparison.Ordinal);
            Assert.DoesNotContain("structurally valid", diagnostics, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void MissingSecretValidation_ExplainsConnectivityAndNavigatesToProviderRepair()
    {
        var initial = CreateConnection();
        initial.Validity = "unknown";
        initial.LatestObservation = new ConnectionObservation
        {
            Status = "succeeded",
            Summary = "Provider metadata was resolved."
        };
        var invalid = CreateConnection();
        invalid.Validity = "invalid";
        invalid.LatestObservation = initial.LatestObservation;
        _api.GetResults.Enqueue(initial);
        _api.GetResults.Enqueue(invalid);
        _api.Adapters = [CreateAdapter(includeSecret: true)];
        _api.ValidationResult = new ConnectionValidationResult
        {
            Valid = false,
            Errors =
            [
                new ConnectionValidationMessage
                {
                    Field = "secretBindings.clientSecret",
                    Code = "required",
                    Message = "A required secret binding is missing."
                }
            ]
        };

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, initial.Id));
        cut.WaitForAssertion(() => Assert.Contains("Re-check configuration", cut.Find(".connection-workspace__diagnostics").TextContent, StringComparison.Ordinal));
        RecheckConfiguration(cut);

        cut.WaitForAssertion(() =>
        {
            var diagnostics = cut.Find(".connection-workspace__diagnostics").TextContent;
            Assert.Contains("Provider connectivity", diagnostics, StringComparison.Ordinal);
            Assert.Contains("Configuration validation", diagnostics, StringComparison.Ordinal);
            Assert.Contains("Client secret", diagnostics, StringComparison.Ordinal);
            Assert.Contains("A required secret binding is missing.", diagnostics, StringComparison.Ordinal);
            Assert.Contains("does not verify complete connection configuration", diagnostics, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Review secret binding", diagnostics, StringComparison.Ordinal);
        });

        cut.Find(".connection-workspace__diagnostics")
            .QuerySelectorAll("button")
            .Single(button => button.TextContent.Contains("Review secret binding", StringComparison.Ordinal))
            .Click();

        cut.WaitForAssertion(() => Assert.Equal(1, cut.FindComponent<MudTabs>().Instance.GetState(component => component.ActivePanelIndex)));
    }

    [Fact]
    public void SuccessfulValidation_RefreshesStatusAndEnablesTheConnection()
    {
        var initial = CreateConnection();
        initial.Validity = "unknown";
        var validated = CreateConnection();
        validated.Validity = "valid";
        _api.GetResults.Enqueue(initial);
        _api.GetResults.Enqueue(validated);
        _api.Adapters = [CreateAdapter()];
        _api.ValidationResult = new ConnectionValidationResult { Valid = true };

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, initial.Id));
        cut.WaitForAssertion(() => Assert.Contains("Draft — not available", cut.Find(".connection-workspace__header").TextContent, StringComparison.Ordinal));
        var displayNameLabel = cut.FindAll("label").Single(element => element.TextContent.Contains("Display name", StringComparison.Ordinal));
        cut.Find($"#{displayNameLabel.GetAttribute("for")}").Change("Unsaved display name");

        RecheckConfiguration(cut);

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(initial.Id, _api.ValidatedConnectionId);
            Assert.Equal([initial.Id, initial.Id], _api.GetRequests);
            Assert.Equal("Unsaved display name", cut.FindComponent<ConnectionEditor>().Instance.Model.DisplayName);
            Assert.Contains("Ready to activate", cut.Find(".connection-workspace__header").TextContent, StringComparison.Ordinal);
            Assert.Contains("The current revision is valid.", cut.Find(".connection-workspace__diagnostics").TextContent, StringComparison.Ordinal);
            Assert.Contains("Save and activate", cut.Find(".connection-workspace__actions").TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Validation_WhenStatusRefreshFails_DoesNotPresentUnverifiedSuccess()
    {
        var initial = CreateConnection();
        initial.Validity = "valid";
        _api.GetResults.Enqueue(initial);
        _api.Adapters = [CreateAdapter()];
        _api.ValidationResult = new ConnectionValidationResult { Valid = true };

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, initial.Id));
        cut.WaitForAssertion(() => Assert.Contains("Ready to activate", cut.Find(".connection-workspace__header").TextContent, StringComparison.Ordinal));

        RecheckConfiguration(cut);

        cut.WaitForAssertion(() =>
        {
            Assert.Equal([initial.Id, initial.Id], _api.GetRequests);
            Assert.Contains("Draft — not available", cut.Find(".connection-workspace__header").TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("The current revision is valid.", cut.Find(".connection-workspace__diagnostics").TextContent, StringComparison.Ordinal);
            Assert.Contains("Check and activate", cut.Find(".connection-workspace__actions").TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ValidationOfAChangedRevision_DoesNotEnableTheConnection()
    {
        var initial = CreateConnection();
        initial.Validity = "valid";
        var changed = CreateConnection();
        changed.Validity = "valid";
        changed.Revision = initial.Revision + 1;
        _api.GetResults.Enqueue(initial);
        _api.GetResults.Enqueue(changed);
        _api.Adapters = [CreateAdapter()];
        _api.ValidationResult = new ConnectionValidationResult { Valid = true };

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, initial.Id));
        cut.WaitForAssertion(() => Assert.Contains("Ready to activate", cut.Find(".connection-workspace__header").TextContent, StringComparison.Ordinal));

        RecheckConfiguration(cut);

        cut.WaitForAssertion(() =>
        {
            Assert.Equal([initial.Id, initial.Id], _api.GetRequests);
            Assert.Contains("Draft — not available", cut.Find(".connection-workspace__header").TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("The current revision is valid.", cut.Find(".connection-workspace__diagnostics").TextContent, StringComparison.Ordinal);
            Assert.Contains(
                "changed while it was being validated",
                Assert.Single(Services.GetRequiredService<ISnackbar>().ShownSnackbars).Message,
                StringComparison.Ordinal);
            Assert.Contains("Check and activate", cut.Find(".connection-workspace__actions").TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task EnableDomainConflict_ShowsItsActionableReasonInsteadOfAConcurrencyDialog()
    {
        var connection = CreateConnection();
        connection.Validity = "valid";
        connection.IsPreferred = true;
        _api.GetResult = connection;
        _api.Adapters = [CreateAdapter()];
        _api.EnableException = await CreateApiExceptionAsync(
            HttpStatusCode.Conflict,
            """{"error":"conflict","message":"The requested connection change conflicts with current state.","details":{"code":"configuration_preferred_connection"}}""");

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, connection.Id));
        cut.WaitForAssertion(() => Assert.Contains("Activate", cut.Find(".connection-workspace__actions").TextContent, StringComparison.Ordinal));

        cut.Find(".connection-workspace__actions")
            .QuerySelectorAll("button")
            .Single(button => button.TextContent.Trim() == "Activate")
            .Click();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Connection changed elsewhere", _dialogProvider.Markup, StringComparison.Ordinal);
            var snackbar = Assert.Single(Services.GetRequiredService<ISnackbar>().ShownSnackbars);
            Assert.Contains("preferred", snackbar.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("clear", snackbar.Message, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void MatchUserPolicy_UsesMatcherDescriptorsAndHidesRawMatcherJson()
    {
        var policy = new UnlinkedIdentityPolicyDescriptor
        {
            Type = "match-user",
            DisplayName = "Match an existing user",
            SettingsVersion = 1,
            Fields =
            [
                new ConnectionFieldDescriptor { Name = "matcher", DisplayName = "User matcher", ValueType = "json" },
                new ConnectionFieldDescriptor { Name = "noMatchAction", DisplayName = "No-match action", ValueType = "string" },
                new ConnectionFieldDescriptor { Name = "defaultRoleIds", DisplayName = "Default roles", ValueType = "string-array" }
            ]
        };
        var value = new PolicySelection
        {
            Type = "match-user",
            SettingsVersion = 1,
            Settings = System.Text.Json.JsonSerializer.SerializeToElement(new
            {
                matcher = new { type = "email", settingsVersion = 1, settings = new { claimType = "email" } },
                noMatchAction = "create-user",
                defaultRoleIds = Array.Empty<string>()
            })
        };
        var matcher = new ExternalUserMatcherDescriptor
        {
            Type = "email",
            DisplayName = "Email matcher",
            SettingsVersion = 1,
            RequiredClaimTypes = ["email"],
            Fields = [new ConnectionFieldDescriptor { Name = "claimType", DisplayName = "Email claim", ValueType = "string" }]
        };

        var cut = Render<ConnectionPolicyEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.Descriptors, [policy])
            .Add(component => component.Matchers, [matcher])
            .Add(component => component.Roles, [new IdentityRoleOption { Id = "role-user", Name = "Users" }])
            .Add(component => component.CanSelectRoles, true));

        Assert.Contains("Required projected claims: email", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("When no user matches", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Versioned matcher selection and settings", cut.Markup, StringComparison.Ordinal);
        Assert.Equal(4, cut.FindComponents<MudSelect<string>>().Count);

        cut.Render(parameters => parameters
            .Add(component => component.Value, null)
            .Add(component => component.Descriptors, [policy])
            .Add(component => component.Matchers, []));
        Assert.DoesNotContain("Match an existing user", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void FullStudioOverride_ClearsSecretBindingsAndSendsExplicitOverrideFlag()
    {
        var connection = CreateConnection("configuration");
        connection.CanCreateOverride = true;
        connection.AdapterSettings["authority"] = System.Text.Json.JsonSerializer.SerializeToElement("https://login.example.test");
        connection.SecretBindings["clientSecret"] = new SecretBindingState
        {
            IsConfigured = true,
            IsResolvable = true
        };
        _api.GetResult = connection;
        _api.Adapters = [CreateAdapter(includeSecret: true)];

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, connection.Id));
        cut.WaitForAssertion(() => Assert.Contains("Create full Database override", cut.Markup, StringComparison.Ordinal));

        cut.FindAll("button").Single(x => x.TextContent.Contains("Create full Database override", StringComparison.Ordinal)).Click();
        Assert.True(cut.FindComponent<Microsoft.AspNetCore.Components.Routing.NavigationLock>().Instance.ConfirmExternalNavigation);
        cut.FindAll("button").Single(x => x.TextContent.Contains("Save as draft", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => Assert.NotNull(_api.CreatedRequest));

        Assert.True(_api.CreatedRequest!.OverridesConfigurationConnection);
        Assert.Equal("host", _api.CreatedRequest.Scope.Kind);
        Assert.DoesNotContain(typeof(ConnectionMutation).GetProperties(), property => property.Name == "SecretBindings");
    }

    [Fact]
    public void StudioOverride_ExplainsDisabledShadowingAndArchiveReveal()
    {
        var connection = CreateConnection();
        connection.OverridesConfigurationConnection = true;
        connection.AdapterSettings["authority"] = System.Text.Json.JsonSerializer.SerializeToElement("https://login.example.test");
        _api.GetResult = connection;
        _api.Adapters = [CreateAdapter()];

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, connection.Id));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("keeps shadowing deployment configuration while disabled", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Archiving it reveals", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("restoring it resumes shadowing", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    public void ShadowedDatabaseConnection_PromotesTheExistingRecordOnlyWhenPermitted(bool canPromote, bool canUpdate, bool expectedAction)
    {
        _permissions.Allowed = canUpdate;
        var connection = CreateConnection();
        connection.Shadowed = true;
        connection.CanPromoteToConfigurationOverride = canPromote;
        connection.SecretBindings["clientSecret"] = new SecretBindingState { IsConfigured = true, IsResolvable = true };
        _api.GetResult = connection;
        _api.Adapters = [CreateAdapter(includeSecret: true)];

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, connection.Id));
        cut.WaitForAssertion(() =>
        {
            if (expectedAction)
                Assert.Contains("Make this Database record effective", cut.Markup, StringComparison.Ordinal);
            else
                Assert.DoesNotContain("Make this Database record effective", cut.Markup, StringComparison.Ordinal);
        });

        if (!expectedAction)
            return;

        cut.FindAll("button").Single(button => button.TextContent.Contains("Make this Database record effective", StringComparison.Ordinal)).Click();
        _dialogProvider.WaitForAssertion(() => Assert.Contains("Make this Database record effective?", _dialogProvider.Markup, StringComparison.Ordinal));
        Assert.Null(_api.UpdatedRequest);

        _dialogProvider.FindAll("button").Single(button => button.TextContent.Contains("Make record effective", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => Assert.NotNull(_api.UpdatedRequest));

        Assert.Equal(connection.Id, _api.UpdatedConnectionId);
        Assert.Equal("\"7\"", _api.UpdatedIfMatch);
        Assert.True(_api.UpdatedRequest!.OverridesConfigurationConnection);
        Assert.Null(_api.CreatedRequest);
        Assert.DoesNotContain(typeof(ConnectionMutation).GetProperties(), property => property.Name == "SecretBindings");
        Assert.True(connection.SecretBindings["clientSecret"].IsConfigured);
    }

    [Fact]
    public async Task ShadowedDatabaseConnection_PromotionRemainsAvailableWithALegacyCustomEditor()
    {
        var connection = CreateConnection();
        connection.Shadowed = true;
        connection.CanPromoteToConfigurationOverride = true;
        _api.GetResult = connection;
        var adapter = CreateAdapter();
        adapter.CustomEditor = new CustomEditorContract { Key = "legacy-editor", ContractVersion = 1 };
        _api.Adapters = [adapter];
        _customEditors.ComponentType = typeof(TestCustomEditor);

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, connection.Id));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Legacy custom editor", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Make this Database record effective", cut.Markup, StringComparison.Ordinal);
            Assert.Equal(
                ["General", "Provisioning", "Diagnostics"],
                cut.FindComponents<MudTabPanel>().Select(panel => panel.Instance.Text));
            Assert.DoesNotContain("Provider", cut.FindComponents<MudTabPanel>().Select(panel => panel.Instance.Text));
        });

        var tabs = cut.FindComponent<MudTabs>();
        var customEditor = cut.FindComponent<TestCustomEditor>().Instance;
        await cut.InvokeAsync(() => tabs.Instance.ActivatePanelAsync(1));
        await cut.InvokeAsync(() => tabs.Instance.ActivatePanelAsync(0));

        Assert.Same(customEditor, cut.FindComponent<TestCustomEditor>().Instance);
    }

    [Fact]
    public void SavedDisabledConnectionWithLegacyCustomEditorRunsCombinedActivation()
    {
        var initial = CreateConnection();
        initial.Validity = "unknown";
        var validated = CreateConnection();
        validated.Validity = "valid";
        var enabled = CreateConnection();
        enabled.Validity = "valid";
        enabled.EnabledIntent = true;
        enabled.EffectivelyEnabled = true;
        _api.GetResults.Enqueue(initial);
        _api.GetResults.Enqueue(validated);
        _api.GetResults.Enqueue(enabled);
        var adapter = CreateAdapter();
        adapter.CustomEditor = new CustomEditorContract { Key = "legacy-editor", ContractVersion = 1 };
        _api.Adapters = [adapter];
        _customEditors.ComponentType = typeof(TestCustomEditor);

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, initial.Id));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Legacy custom editor", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Check and activate", cut.Find(".connection-workspace__actions").TextContent, StringComparison.Ordinal);
        });
        cut.Find(".connection-workspace__actions").QuerySelectorAll("button").Single(button => button.TextContent.Contains("Check and activate", StringComparison.Ordinal)).Click();

        cut.WaitForAssertion(() => Assert.Equal(initial.Id, _api.EnabledConnectionId));
        Assert.Equal(initial.Id, _api.ValidatedConnectionId);
    }

    [Fact]
    public async Task ChangeTrackingCustomEditor_MarksConnectionAsDirty()
    {
        var connection = CreateConnection();
        _api.GetResult = connection;
        var adapter = CreateAdapter();
        adapter.CustomEditor = new CustomEditorContract { Key = "tracked-editor", ContractVersion = 1 };
        _api.Adapters = [adapter];
        _customEditors.ComponentType = typeof(ChangeTrackingCustomEditor);

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, connection.Id));

        cut.WaitForAssertion(() => Assert.Contains("Change-tracking custom editor", cut.Markup, StringComparison.Ordinal));
        Assert.False(cut.FindComponent<Microsoft.AspNetCore.Components.Routing.NavigationLock>().Instance.ConfirmExternalNavigation);

        var editor = cut.FindComponent<ChangeTrackingCustomEditor>().Instance;
        await cut.InvokeAsync(() => editor.Changed.InvokeAsync());

        Assert.True(cut.FindComponent<Microsoft.AspNetCore.Components.Routing.NavigationLock>().Instance.ConfirmExternalNavigation);
    }

    [Fact]
    public async Task Diagnostics_OmitsBackendBuildMetadataAndDoesNotLoadIt()
    {
        var connection = CreateConnection();
        _api.GetResult = connection;
        _api.Adapters = [CreateAdapter()];
        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, connection.Id));
        var tabs = cut.FindComponent<MudTabs>();
        await cut.InvokeAsync(() => tabs.Instance.ActivatePanelAsync(3));

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Management contract", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Backend test", cut.Markup, StringComparison.Ordinal);
            Assert.Equal(0, _api.RuntimeRequests);
        });
    }

    [Fact]
    public void ShadowedConnection_ExplainsWhyThePersistedRecordIsNotEffective()
    {
        var connection = CreateConnection();
        connection.Shadowed = true;
        _api.GetResult = connection;
        _api.Adapters = [CreateAdapter()];

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, connection.Id));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("shadowed by deployment configuration", cut.Markup, StringComparison.OrdinalIgnoreCase);
            var header = cut.Find(".connection-workspace__header");
            Assert.Contains("Effective: Deployment", header.TextContent, StringComparison.Ordinal);
            Assert.Contains("Draft — not available", header.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("Stored record not tested", header.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("At a glance", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ShadowedConfigurationConnection_ExplainsThatStudioIsEffective()
    {
        var connection = CreateConnection("configuration");
        connection.Shadowed = true;
        connection.EnabledIntent = true;
        connection.Validity = "valid";
        connection.CanCreateOverride = true;
        _api.GetResult = connection;
        _api.Adapters = [CreateAdapter()];
        _api.ListResults.Enqueue(new ListConnectionsResponse
        {
            Items =
            [
                new ConnectionSummary
                {
                    Id = "studio-override",
                    Key = connection.Key,
                    Source = "database",
                    Scope = connection.Scope
                }
            ]
        });

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, connection.Id));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("deployment-defined connection is read-only and shadowed by an effective Database override", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("persisted record is shadowed by deployment configuration", cut.Markup, StringComparison.OrdinalIgnoreCase);
            var header = cut.Find(".connection-workspace__header");
            Assert.Contains("Effective: Database", header.TextContent, StringComparison.Ordinal);
            Assert.Contains("Not available for sign-in", header.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("At a glance", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void SecretBinding_ShowsConfiguredStateWithoutRevealingASecretValue()
    {
        var connection = CreateConnection();
        connection.SecretBindings["clientSecret"] = new SecretBindingState
        {
            IsConfigured = true,
            IsResolvable = true
        };

        var cut = Render<ConnectionEditor>(parameters => parameters
            .Add(component => component.Connection, connection)
            .Add(component => component.Adapter, CreateAdapter(includeSecret: true))
            .Add(component => component.Model, CreateMutation())
            .Add(component => component.ReadOnly, true));

        Assert.Contains("Configured and resolvable", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("super-secret-value", cut.Markup);
        Assert.DoesNotContain("contoso-client-secret", cut.Markup);
    }

    [Fact]
    public void EnabledConnection_CannotRemoveARequiredSecretBinding()
    {
        var connection = CreateConnection();
        connection.EnabledIntent = true;
        connection.SecretBindings["clientSecret"] = new SecretBindingState { Ownership = "managed", IsConfigured = true, IsResolvable = true };
        var cut = Render<ConnectionEditor>(parameters => parameters
            .Add(component => component.Connection, connection)
            .Add(component => component.Adapter, CreateAdapter(includeSecret: true))
            .Add(component => component.Model, CreateMutation()));

        Assert.Contains("cannot be removed while the connection is enabled", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.True(cut.FindAll("button").Single(x => x.TextContent.Contains("Remove secret binding", StringComparison.Ordinal)).HasAttribute("disabled"));
    }

    [Fact]
    public void ManagedSecret_IsWriteOnlyAndExternalBindingsAreNotEditableInStudio()
    {
        ManagedSecretMutation? replaced = null;
        var cut = Render<SecretBindingField>(parameters => parameters
            .Add(component => component.Field, new ConnectionFieldDescriptor { Name = "clientSecret", DisplayName = "Client secret", IsSecretBinding = true })
            .Add(component => component.ManagedSecretResolvers,
            [
                new ManagedSecretResolverDescriptor { Type = "custom-vault", DisplayName = "Custom vault" },
                new ManagedSecretResolverDescriptor { Type = "elsa-secrets", DisplayName = "Elsa Secrets" }
            ])
            .Add(component => component.OnManagedReplace, EventCallback.Factory.Create<ManagedSecretMutation>(this, value => replaced = value)));

        Assert.Contains("Managed secret", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("External binding", cut.Markup, StringComparison.Ordinal);
        Assert.All(cut.FindComponents<MudTextField<string>>(), field =>
            AssertOutlinedDense(field.Instance.Variant, field.Instance.Margin));
        Assert.All(cut.FindComponents<MudSelect<string>>(), field =>
            AssertOutlinedDense(field.Instance.Variant, field.Instance.Margin));
        var password = cut.Find("input[type=password]");
        password.Change("top-secret-value");
        cut.FindAll("button").Single(x => x.TextContent.Contains("Replace managed secret", StringComparison.Ordinal)).Click();

        Assert.NotNull(replaced);
        Assert.Equal("elsa-secrets", replaced!.ResolverType);
        Assert.Equal("top-secret-value", replaced.Value);
        Assert.DoesNotContain("top-secret-value", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ManagedSecretOption_IsHiddenWhenBackendCapabilityIsUnavailable()
    {
        var cut = Render<SecretBindingField>(parameters => parameters
            .Add(component => component.Field, new ConnectionFieldDescriptor { Name = "clientSecret", DisplayName = "Client secret", IsSecretBinding = true })
            .Add(component => component.ManagedSecretResolverError, "Managed secret storage is unavailable."));

        Assert.Contains("Managed secret storage is unavailable", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Replace managed secret", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("External binding", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void SecretRemovalPrompt_DistinguishesManagedDeletionFromExternalReferenceRemoval()
    {
        var managed = SecretBindingRemovalPrompt.GetMessage(new SecretBindingState { Ownership = "managed" });
        var external = SecretBindingRemovalPrompt.GetMessage(new SecretBindingState { Ownership = "external" });

        Assert.Contains("secret value", managed, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("deleted", managed, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("reference", external, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not deleted", external, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FinalLoginPathGuard_IsDistinguishedFromOrdinaryConcurrencyConflict()
    {
        Assert.True(ConnectionManagementError.IsFinalLoginPathGuard(
            System.Net.HttpStatusCode.Conflict,
            """{"details":{"code":"final_login_path_guard"}}"""));
        Assert.False(ConnectionManagementError.IsFinalLoginPathGuard(
            System.Net.HttpStatusCode.Conflict,
            """{"error":"conflict"}"""));
    }

    [Fact]
    public void UnsafeConfirmation_IsOnlyShownWhenAnUnsafeSettingIsActive()
    {
        var model = CreateMutation();
        var cut = Render<ConnectionEditor>(parameters => parameters
            .Add(component => component.Connection, CreateConnection())
            .Add(component => component.Adapter, CreateAdapter(includeUnsafe: true))
            .Add(component => component.Model, model)
            .Add(component => component.CanConfigureUnsafeSettings, false));

        Assert.DoesNotContain("I understand and explicitly confirm", cut.Markup, StringComparison.Ordinal);

        DescriptorEditorState.SetBoolean(model.AdapterSettings, "allowInsecureMetadata", true);
        cut.Render();

        Assert.Contains("requires explicit confirmation", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("do not have permission", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DescriptorState_PreservesTypedJsonValuesAndEvaluatesVisibilityAndValidation()
    {
        var settings = new Dictionary<string, System.Text.Json.JsonElement>(StringComparer.Ordinal);
        DescriptorEditorState.SetBoolean(settings, "enabled", true);
        DescriptorEditorState.SetInteger(settings, "retries", 3);
        DescriptorEditorState.SetNumber(settings, "timeout", 1.5m);
        DescriptorEditorState.SetString(settings, "authority", "https://login.example.test");

        Assert.True(DescriptorEditorState.GetBoolean(settings, "enabled"));
        Assert.Equal(3, DescriptorEditorState.GetInteger(settings, "retries"));
        Assert.Equal(1.5m, DescriptorEditorState.GetNumber(settings, "timeout"));
        Assert.Equal("https://login.example.test", DescriptorEditorState.ToDisplayString(settings["authority"]));

        var conditional = new ConnectionFieldDescriptor { Name = "clientId", DisplayName = "Client ID", VisibleWhen = new ConnectionFieldVisibilityCondition { Field = "enabled", ExpectedValue = "true" } };
        Assert.True(DescriptorEditorState.IsVisible(conditional, settings));

        var constrained = new ConnectionFieldDescriptor
        {
            Name = "authority",
            DisplayName = "Authority",
            ValueType = "uri",
            IsRequired = true,
            Validation = new ConnectionFieldValidation { MaximumLength = 10, Pattern = "^https://" }
        };
        Assert.NotEmpty(DescriptorEditorState.Validate(constrained, settings));
    }

    [Fact]
    public void AllowedValues_PreserveTheDescriptorValueType()
    {
        var settings = new Dictionary<string, System.Text.Json.JsonElement>(StringComparer.Ordinal);
        var integer = new ConnectionFieldDescriptor { Name = "retries", DisplayName = "Retries", ValueType = "integer" };
        var number = new ConnectionFieldDescriptor { Name = "timeout", DisplayName = "Timeout", ValueType = "number" };
        var boolean = new ConnectionFieldDescriptor { Name = "enabled", DisplayName = "Enabled", ValueType = "boolean" };

        Assert.True(DescriptorEditorState.TrySetAllowedValue(settings, integer, "3", out _));
        Assert.True(DescriptorEditorState.TrySetAllowedValue(settings, number, "1.5", out _));
        Assert.True(DescriptorEditorState.TrySetAllowedValue(settings, boolean, "true", out _));

        Assert.Equal(System.Text.Json.JsonValueKind.Number, settings["retries"].ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.Number, settings["timeout"].ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.True, settings["enabled"].ValueKind);
    }

    [Fact]
    public void StructuredValues_RejectInvalidJsonAndNonStringArraysWithoutChangingThePriorValue()
    {
        var settings = new Dictionary<string, System.Text.Json.JsonElement> { ["claims"] = System.Text.Json.JsonSerializer.SerializeToElement(new[] { "name" }) };

        Assert.False(DescriptorEditorState.TrySetStructuredValue(settings, "claims", "{broken", true, out var invalidJson));
        Assert.Contains("valid JSON", invalidJson, StringComparison.OrdinalIgnoreCase);
        Assert.False(DescriptorEditorState.TrySetStructuredValue(settings, "claims", "[1]", true, out var wrongShape));
        Assert.Contains("only strings", wrongShape, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(System.Text.Json.JsonValueKind.Array, settings["claims"].ValueKind);
        Assert.Equal("name", settings["claims"].EnumerateArray().Single().GetString());
    }

    [Fact]
    public void CustomEditorRegistry_UsesExactCompatibleContractAndOtherwiseFallsBack()
    {
        var registry = new CustomConnectionEditorRegistry([new CustomEditorRegistration("oidc-editor", 2, typeof(TestCustomEditor))]);

        Assert.True(registry.TryResolve(new CustomEditorContract { Key = "oidc-editor", ContractVersion = 2 }, out var selected));
        Assert.Equal(typeof(TestCustomEditor), selected);
        Assert.False(registry.TryResolve(new CustomEditorContract { Key = "oidc-editor", ContractVersion = 1 }, out _));
        Assert.False(registry.TryResolve(null, out _));
    }

    [Fact]
    public void CustomEditorRegistry_RejectsDuplicateContractsAndHelperRegistersTheEditor()
    {
        Assert.Throws<InvalidOperationException>(() => new CustomConnectionEditorRegistry(
        [
            new CustomEditorRegistration("oidc-editor", 1, typeof(TestCustomEditor)),
            new CustomEditorRegistration("oidc-editor", 1, typeof(TestCustomEditor))
        ]));

        var services = new ServiceCollection();
        services.AddExternalAuthenticationCustomEditor<TestCustomEditor>("oidc-editor", 1);
        using var provider = services.BuildServiceProvider();
        var registrations = provider.GetServices<ICustomConnectionEditorRegistration>();
        var registry = new CustomConnectionEditorRegistry(registrations);
        Assert.True(registry.TryResolve(new CustomEditorContract { Key = "oidc-editor", ContractVersion = 1 }, out _));
    }

    [Fact]
    public void AdapterSelection_ResetsAdapterOwnedSettingsAndSecretBindingState()
    {
        var connection = CreateConnection();
        connection.AdapterSettings["oldSetting"] = System.Text.Json.JsonSerializer.SerializeToElement("old");
        connection.SecretBindings["oldSecret"] = new SecretBindingState { IsConfigured = true };
        var mutation = CreateMutation();
        mutation.AdapterSettings["oldSetting"] = System.Text.Json.JsonSerializer.SerializeToElement("old");
        mutation.ConfirmUnsafeSettings = true;
        var replacement = new AdapterDescriptor
        {
            Type = "custom-oauth",
            SettingsVersion = 4,
            Fields = [new ConnectionFieldDescriptor { Name = "enabled", DisplayName = "Enabled", ValueType = "boolean", DefaultValue = System.Text.Json.JsonSerializer.SerializeToElement(true) }]
        };

        ConnectionAdapterSelection.Apply(connection, mutation, replacement);

        Assert.Equal("custom-oauth", mutation.AdapterType);
        Assert.Equal(System.Text.Json.JsonValueKind.True, mutation.AdapterSettings["enabled"].ValueKind);
        Assert.Empty(connection.SecretBindings);
        Assert.DoesNotContain("oldSetting", mutation.AdapterSettings.Keys);
        Assert.False(mutation.ConfirmUnsafeSettings);
    }

    [Fact]
    public void ConnectionListPagingState_NavigatesBothDirectionsAndResetsToTheFirstPage()
    {
        var paging = new ConnectionListPagingState();

        paging.Replace("cursor-2");
        Assert.Equal(1, paging.PageNumber);
        Assert.False(paging.HasPrevious);
        Assert.True(paging.HasNext);
        Assert.True(paging.TryAdvance(out var secondPageCursor));
        Assert.Equal("cursor-2", secondPageCursor);
        Assert.Equal(2, paging.PageNumber);
        paging.SetNext("cursor-3");

        Assert.True(paging.TryGoBack(out var firstPageCursor));
        Assert.Null(firstPageCursor);
        Assert.Equal(1, paging.PageNumber);
        Assert.True(paging.HasNext);
        Assert.True(paging.TryAdvance(out var restoredSecondPageCursor));
        Assert.Equal("cursor-2", restoredSecondPageCursor);
        Assert.Equal(2, paging.PageNumber);
        Assert.True(paging.HasNext);

        paging.Replace("fresh-cursor-2");
        Assert.Equal(1, paging.PageNumber);
        Assert.False(paging.HasPrevious);
        Assert.Equal("fresh-cursor-2", paging.NextCursor);
    }

    [Fact]
    public void ConnectionListRequestState_RejectsAnOutOfOrderResponse()
    {
        var requests = new ConnectionListRequestState();

        var initialRequest = requests.Begin();
        var filteredRequest = requests.Begin();

        Assert.False(requests.IsCurrent(initialRequest));
        Assert.True(requests.IsCurrent(filteredRequest));
    }

    [Fact]
    public void ConnectionListAndLifecycleAffordances_RespectCursorOwnershipAndPermissions()
    {
        var paging = new ConnectionListPagingState();
        paging.Replace("cursor-2");
        Assert.True(paging.TryAdvance(out var cursor));
        Assert.Equal("cursor-2", cursor);
        paging.SetNext(null);
        Assert.False(paging.TryAdvance(out _));

        var configuration = CreateConnection("configuration");
        var database = CreateConnection();
        database.Validity = "valid";
        Assert.False(ConnectionActionAvailability.CanEnableOrDisable(configuration, true));
        Assert.False(ConnectionActionAvailability.CanArchiveOrRestore(configuration, true));
        Assert.True(ConnectionActionAvailability.CanEnableOrDisable(database, true));
        Assert.True(ConnectionActionAvailability.CanArchiveOrRestore(database, true));
        database.Validity = "invalid";
        Assert.False(ConnectionActionAvailability.CanEnableOrDisable(database, true));
        database.Validity = "valid";
        database.Archived = true;
        Assert.False(ConnectionActionAvailability.CanEnableOrDisable(database, true));
        Assert.Equal("\"17\"", ConnectionConcurrency.ToIfMatch(17));
        Assert.True(ConnectionConcurrency.IsConflict(System.Net.HttpStatusCode.PreconditionFailed));
        Assert.False(ConnectionConcurrency.IsConflict(System.Net.HttpStatusCode.Conflict));
        var changedElsewhere = new ManagementApiException("changed", 412, CreateConnection());
        Assert.True(ConnectionConflictRecovery.TryGetCurrent(changedElsewhere, out var recovered));
        Assert.Equal("connection-1", recovered.Id);
    }

    [Fact]
    public void GenericEditor_PreventsSaveAndDisplaysDescriptorValidationErrors()
    {
        var saved = false;
        var cut = Render<ConnectionEditor>(parameters => parameters
            .Add(component => component.Connection, CreateConnection())
            .Add(component => component.Adapter, CreateAdapter())
            .Add(component => component.Model, CreateMutation())
            .Add(component => component.Saved, EventCallback.Factory.Create<ConnectionMutation>(this, _ => saved = true)));

        cut.FindAll("button").Single(button => button.TextContent.Contains("Save changes", StringComparison.Ordinal)).Click();

        Assert.False(saved);
        Assert.Contains("Authority is required", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void GenericEditor_BlocksSavingAfterInvalidJsonInput()
    {
        var saved = false;
        var mutation = CreateMutation();
        mutation.AdapterSettings["projection"] = System.Text.Json.JsonSerializer.SerializeToElement(new[] { "name" });
        var adapter = new AdapterDescriptor { Type = "custom", DisplayName = "Custom", Fields = [new ConnectionFieldDescriptor { Name = "projection", DisplayName = "Claim projection", ValueType = "json" }] };
        var cut = Render<ConnectionEditor>(parameters => parameters
            .Add(component => component.Connection, CreateConnection())
            .Add(component => component.Adapter, adapter)
            .Add(component => component.Model, mutation)
            .Add(component => component.Saved, EventCallback.Factory.Create<ConnectionMutation>(this, _ => saved = true)));

        cut.Find("textarea").Change("{broken");
        cut.FindAll("button").Single(button => button.TextContent.Contains("Save changes", StringComparison.Ordinal)).Click();

        Assert.False(saved);
        Assert.Contains("valid JSON", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateShowsStructuredServerValidationDetails()
    {
        var adapter = CreateAdapter();
        adapter.Fields.First().DefaultValue = JsonSerializer.SerializeToElement("https://issuer.example.test");
        _api.Adapters = [adapter];
        _api.CreateException = await CreateApiExceptionAsync(
            HttpStatusCode.BadRequest,
            """
            {
              "error": "validation_failed",
              "message": "The connection is not valid for this operation.",
              "details": {
                "errors": [
                  {
                    "field": "adapterSettings.authority",
                    "code": "invalid",
                    "message": "Authority must use HTTPS."
                  }
                ],
                "warnings": []
              }
            }
            """);

        var cut = Render<ConnectionEdit>();
        cut.WaitForAssertion(() => Assert.Contains("Create identity provider connection", cut.Markup, StringComparison.Ordinal));
        ChangeField("Display name", "Contoso");
        ChangeField("Connection key", "contoso");
        cut.FindAll("button").Single(button => button.TextContent.Contains("Save as draft", StringComparison.Ordinal)).Click();

        var snackbar = Services.GetRequiredService<ISnackbar>();
        cut.WaitForAssertion(() =>
        {
            var message = Assert.Single(snackbar.ShownSnackbars).Message;
            Assert.Contains("The connection is not valid for this operation.", message, StringComparison.Ordinal);
            Assert.Contains("adapterSettings.authority: Authority must use HTTPS.", message, StringComparison.Ordinal);
            Assert.DoesNotContain("Response status code does not indicate success", message, StringComparison.Ordinal);
        });

        void ChangeField(string label, string value)
        {
            var fieldLabel = cut.FindAll("label").Single(element => element.TextContent.Contains(label, StringComparison.Ordinal));
            cut.Find($"#{fieldLabel.GetAttribute("for")}").Change(value);
        }
    }

    [Fact]
    public void ConnectionList_UsesTheServerCursorForTheNextPage()
    {
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [CreateConnection()], NextCursor = "cursor-2" });
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [new ConnectionSummary { Id = "connection-2", Key = "next", DisplayName = "Next", AdapterType = "custom" }], NextCursor = null });
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [CreateConnection()], NextCursor = "cursor-2" });
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [new ConnectionSummary { Id = "connection-2", Key = "next", DisplayName = "Next", AdapterType = "custom" }], NextCursor = null });
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [CreateConnection()], NextCursor = "cursor-2" });

        var cut = Render<ConnectionIndex>();
        cut.WaitForAssertion(() => Assert.Contains("Contoso", cut.Markup));
        var pager = cut.Find(".mud-table-pagination");
        Assert.Contains("Rows Per Page", pager.TextContent, StringComparison.Ordinal);
        Assert.Contains("Page 1", pager.TextContent, StringComparison.Ordinal);
        var firstPage = pager.QuerySelector("button[aria-label=\"First page\"]")!;
        var previousPage = pager.QuerySelector("button[aria-label=\"Previous page\"]")!;
        var nextPage = pager.QuerySelector("button[aria-label=\"Next page\"]")!;
        Assert.True(firstPage.HasAttribute("disabled"));
        Assert.True(previousPage.HasAttribute("disabled"));
        Assert.False(nextPage.HasAttribute("disabled"));
        nextPage.Click();
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Next", cut.Markup);
            var nextPager = cut.Find(".mud-table-pagination");
            Assert.Contains("Page 2", nextPager.TextContent, StringComparison.Ordinal);
            Assert.False(nextPager.QuerySelector("button[aria-label=\"First page\"]")!.HasAttribute("disabled"));
            Assert.False(nextPager.QuerySelector("button[aria-label=\"Previous page\"]")!.HasAttribute("disabled"));
            Assert.True(nextPager.QuerySelector("button[aria-label=\"Next page\"]")!.HasAttribute("disabled"));
            Assert.Null(nextPager.QuerySelector("button[aria-label=\"Last page\"]"));
        });

        cut.Find("button[aria-label=\"Previous page\"]").Click();
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Contoso", cut.Markup, StringComparison.Ordinal);
            var previousPager = cut.Find(".mud-table-pagination");
            Assert.Contains("Page 1", previousPager.TextContent, StringComparison.Ordinal);
            Assert.True(previousPager.QuerySelector("button[aria-label=\"Previous page\"]")!.HasAttribute("disabled"));
        });

        cut.Find("button[aria-label=\"Next page\"]").Click();
        cut.WaitForAssertion(() => Assert.Contains("Page 2", cut.Find(".mud-table-pagination").TextContent, StringComparison.Ordinal));
        cut.Find("button[aria-label=\"First page\"]").Click();
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Contoso", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Page 1", cut.Find(".mud-table-pagination").TextContent, StringComparison.Ordinal);
        });

        Assert.Equal([null, "cursor-2", null, "cursor-2", null], _api.Cursors);
    }

    [Fact]
    public async Task ConnectionList_ChangingPageSizeResetsTheCursorChain()
    {
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [CreateConnection()], NextCursor = "cursor-2" });
        _api.ListResults.Enqueue(new ListConnectionsResponse
        {
            Items = [new ConnectionSummary { Id = "connection-2", Key = "next", DisplayName = "Next", AdapterType = "custom" }],
            NextCursor = "cursor-3"
        });
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [CreateConnection()], NextCursor = null });

        var cut = Render<ConnectionIndex>();
        cut.WaitForAssertion(() => Assert.Contains("Contoso", cut.Markup, StringComparison.Ordinal));
        cut.Find("button[aria-label=\"Next page\"]").Click();
        cut.WaitForAssertion(() => Assert.Contains("Next", cut.Markup, StringComparison.Ordinal));

        var pageSize = cut.FindComponent<MudSelect<int>>().Instance;
        Assert.Equal([10, 25, 50, 100], cut.FindComponents<MudSelectItem<int>>().Select(item => item.Instance.Value));
        await cut.InvokeAsync(() => pageSize.ValueChanged.InvokeAsync(25));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Contoso", cut.Markup, StringComparison.Ordinal);
            Assert.True(cut.Find("button[aria-label=\"First page\"]").HasAttribute("disabled"));
            Assert.Equal(
                [(null, 10), ("cursor-2", 10), (null, 25)],
                _api.ListRequests.Select(request => (request.Cursor, request.PageSize)));
        });
    }

    [Fact]
    public void ConnectionList_ExposesLabeledFiltersStatusAndNamedActions()
    {
        var connection = CreateConnection();
        connection.EnabledIntent = true;
        connection.EffectivelyEnabled = true;
        connection.Validity = "valid";
        connection.OverridesConfigurationConnection = true;
        connection.IsPreferred = true;
        _api.Adapters =
        [
            new AdapterDescriptor
            {
                Type = connection.AdapterType,
                Capabilities = new AdapterCapabilities { SupportsTest = true, SupportsPreview = true }
            }
        ];
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [connection] });

        var cut = Render<ConnectionIndex>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Identity provider connections", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Search connections", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Include archived", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Ownership", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Availability", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Database", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Overrides deployment", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Available", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Enabled · Valid", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Preferred", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain(
                cut.FindComponents<MudChip<string>>(),
                chip => chip.Markup.Contains("Preferred", StringComparison.Ordinal));
            Assert.NotEmpty(cut.FindAll("[aria-label=\"Actions\"]"));
            var actions = Assert.Single(cut.FindComponents<MudMenu>());
            Assert.Equal($"Actions for {connection.DisplayName}", actions.Instance.AriaLabel);
        });

        cut.Find($"button[aria-label=\"Actions for {connection.DisplayName}\"]").Click();
        _popoverProvider.WaitForAssertion(() =>
        {
            Assert.Contains("Manage", _popoverProvider.Markup, StringComparison.Ordinal);
            Assert.Contains("Test connection", _popoverProvider.Markup, StringComparison.Ordinal);
            Assert.Contains("Preview sign-in", _popoverProvider.Markup, StringComparison.Ordinal);
            Assert.Contains("Disable", _popoverProvider.Markup, StringComparison.Ordinal);
            Assert.Contains("Archive", _popoverProvider.Markup, StringComparison.Ordinal);
            Assert.Equal(2, _popoverProvider.FindAll(".mud-divider").Count);
        });
    }

    [Fact]
    public void ConnectionList_NamesAndLinksBothSidesOfAShadowRelationship()
    {
        var deployment = CreateConnection("configuration");
        deployment.Id = "deployment-keycloak";
        deployment.DisplayName = "Keycloak General";
        deployment.Shadowed = true;
        deployment.ShadowedBy = new ConnectionReference
        {
            Id = "database-keycloak",
            DisplayName = "Keycloak",
            Source = "database"
        };
        var database = CreateConnection();
        database.Id = "database-keycloak";
        database.DisplayName = "Keycloak";
        database.OverridesConfigurationConnection = true;
        database.Shadows =
        [
            new ConnectionReference
            {
                Id = deployment.Id,
                DisplayName = deployment.DisplayName,
                Source = "configuration"
            }
        ];
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [database, deployment] });

        var cut = Render<ConnectionIndex>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Shadowed by", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Overrides", cut.Markup, StringComparison.Ordinal);
            var shadowingLink = cut.Find("a[aria-label=\"Manage shadowing connection Keycloak\"]");
            Assert.EndsWith(
                "security/external-authentication/connections/database-keycloak",
                shadowingLink.GetAttribute("href"),
                StringComparison.Ordinal);
            var shadowedLink = cut.Find("a[aria-label=\"Manage shadowed connection Keycloak General\"]");
            Assert.Contains("Overrides Keycloak General", shadowedLink.ParentElement!.ParentElement!.TextContent, StringComparison.Ordinal);
            Assert.EndsWith(
                "security/external-authentication/connections/deployment-keycloak",
                shadowedLink.GetAttribute("href"),
                StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ConnectionList_DoesNotPresentAnActiveShadowRelationshipWhenTheServerOmitsIt()
    {
        var deployment = CreateConnection("configuration");
        deployment.Id = "deployment-keycloak";
        deployment.DisplayName = "Keycloak General";
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [deployment] });

        var cut = Render<ConnectionIndex>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(deployment.DisplayName, cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Shadows", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Shadowed by", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ConnectionList_ClickingARowOpensTheManageScreen()
    {
        var connection = CreateConnection();
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [connection] });
        var cut = Render<ConnectionIndex>();
        cut.WaitForAssertion(() => Assert.Contains(connection.DisplayName, cut.Markup, StringComparison.Ordinal));

        var manageLink = cut.Find($"a[aria-label=\"Manage {connection.DisplayName}\"]");
        Assert.Equal($"security/external-authentication/connections/{connection.Id}", manageLink.GetAttribute("href"));
        cut.Find("tbody tr").Click();

        Assert.EndsWith(
            $"/security/external-authentication/connections/{connection.Id}",
            Services.GetRequiredService<NavigationManager>().Uri,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ConnectionList_ActionsMenuDoesNotTriggerTheRowAction()
    {
        var connection = CreateConnection();
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [connection] });
        var navigation = Services.GetRequiredService<NavigationManager>();
        var initialUri = navigation.Uri;
        var cut = Render<ConnectionIndex>();
        cut.WaitForAssertion(() => Assert.Contains(connection.DisplayName, cut.Markup, StringComparison.Ordinal));

        cut.Find($"button[aria-label=\"Actions for {connection.DisplayName}\"]").Click();

        _popoverProvider.WaitForAssertion(() => Assert.Contains("Manage", _popoverProvider.Markup, StringComparison.Ordinal));
        Assert.Equal(initialUri, navigation.Uri);
    }

    [Fact]
    public void ConnectionList_PresentsLatestTestAsASemanticStatusAndSecondarySummary()
    {
        const string summary = "Provider metadata was resolved.";
        var connection = CreateConnection();
        connection.LatestObservation = new ConnectionObservation
        {
            Status = "succeeded",
            Summary = summary,
            IsStale = true
        };
        var untestedConnection = CreateConnection();
        untestedConnection.Id = "connection-2";
        untestedConnection.DisplayName = "Untested";
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [connection, untestedConnection] });

        var cut = Render<ConnectionIndex>();

        cut.WaitForAssertion(() =>
        {
            var status = cut.FindComponents<MudChip<string>>()
                .Single(chip => chip.Markup.Contains("Succeeded · Stale", StringComparison.Ordinal));
            Assert.Equal(Color.Warning, status.Instance.Color);
            Assert.Equal(Icons.Material.Outlined.ReportProblem, status.Instance.Icon);

            var summaryText = cut.FindComponents<MudText>()
                .Single(text => text.Markup.Contains(summary, StringComparison.Ordinal));
            Assert.Equal(Color.Secondary, summaryText.Instance.Color);

            var notTested = cut.FindComponents<MudText>()
                .Single(text => text.Markup.Contains("Not tested", StringComparison.Ordinal));
            Assert.Equal(Color.Secondary, notTested.Instance.Color);
            Assert.DoesNotContain(
                cut.FindComponents<MudChip<string>>(),
                chip => chip.Markup.Contains("Not tested", StringComparison.Ordinal));
        });
    }

    [Fact]
    public void ConnectionList_TestConnectionRecordsTheObservationAndShowsTheRedactedSuccessMessage()
    {
        var connection = CreateConnection();
        _api.Adapters =
        [
            new AdapterDescriptor
            {
                Type = connection.AdapterType,
                Capabilities = new AdapterCapabilities { SupportsTest = true }
            }
        ];
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [connection] });

        var cut = Render<ConnectionIndex>();
        cut.WaitForAssertion(() => Assert.Contains(connection.DisplayName, cut.Markup, StringComparison.Ordinal));

        cut.Find($"button[aria-label=\"Actions for {connection.DisplayName}\"]").Click();
        _popoverProvider.WaitForElements(".mud-menu-item")
            .Single(item => item.TextContent.Contains("Test connection", StringComparison.Ordinal))
            .Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(connection.Id, _operations.TestedConnectionId);
            Assert.Equal("\"7\"", _operations.TestedIfMatch);
            Assert.Contains("Provider metadata was resolved.", cut.Markup, StringComparison.Ordinal);
            Assert.Equal(
                "Connection test completed. The result contains only redacted diagnostics.",
                Assert.Single(Services.GetRequiredService<ISnackbar>().ShownSnackbars).Message);
        });
    }

    [Fact]
    public void ConnectionList_FailedTestUsesTheSameRedactedOperationalMessageAsTheDiagnosticsPage()
    {
        var connection = CreateConnection();
        _api.Adapters =
        [
            new AdapterDescriptor
            {
                Type = connection.AdapterType,
                Capabilities = new AdapterCapabilities { SupportsTest = true }
            }
        ];
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [connection] });
        _operations.TestException = new InvalidOperationException("provider-access-token");

        var cut = Render<ConnectionIndex>();
        cut.WaitForAssertion(() => Assert.Contains(connection.DisplayName, cut.Markup, StringComparison.Ordinal));

        cut.Find($"button[aria-label=\"Actions for {connection.DisplayName}\"]").Click();
        _popoverProvider.WaitForElements(".mud-menu-item")
            .Single(item => item.TextContent.Contains("Test connection", StringComparison.Ordinal))
            .Click();

        cut.WaitForAssertion(() =>
        {
            var message = Assert.Single(Services.GetRequiredService<ISnackbar>().ShownSnackbars).Message;
            Assert.Equal(ConnectionOperationActions.TestFailedMessage, message);
            Assert.DoesNotContain("provider-access-token", message, StringComparison.Ordinal);
            Assert.Contains("Not tested", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ConnectionList_ArchiveRequiresConfirmationAndReloadsTheList()
    {
        var connection = CreateConnection();
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [connection] });
        _api.ListResults.Enqueue(new ListConnectionsResponse());

        var cut = Render<ConnectionIndex>();
        cut.WaitForAssertion(() => Assert.Contains(connection.DisplayName, cut.Markup, StringComparison.Ordinal));

        cut.Find($"button[aria-label=\"Actions for {connection.DisplayName}\"]").Click();
        _popoverProvider.WaitForElements(".mud-menu-item")
            .Single(item => item.TextContent.Contains("Archive", StringComparison.Ordinal))
            .Click();
        _dialogProvider.WaitForAssertion(() =>
            Assert.Contains("preserves its identity links", _dialogProvider.Markup, StringComparison.Ordinal));
        _dialogProvider.FindAll("button")
            .Single(button => button.TextContent.Trim() == "Archive")
            .Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(connection.Id, _api.ArchivedConnectionId);
            Assert.Equal("\"7\"", _api.ArchivedIfMatch);
            Assert.Equal(2, _api.ListRequests.Count);
            Assert.DoesNotContain(connection.DisplayName, cut.Markup, StringComparison.Ordinal);
            Assert.Equal(
                "Connection archived.",
                Assert.Single(Services.GetRequiredService<ISnackbar>().ShownSnackbars).Message);
        });
    }

    [Fact]
    public void ConnectionList_PreviewSignInPreservesTheOneTimeResultFlowInADialog()
    {
        var connection = CreateConnection();
        _api.Adapters =
        [
            new AdapterDescriptor
            {
                Type = connection.AdapterType,
                Capabilities = new AdapterCapabilities { SupportsPreview = true }
            }
        ];
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [connection] });

        var cut = Render<ConnectionIndex>();
        cut.WaitForAssertion(() => Assert.Contains(connection.DisplayName, cut.Markup, StringComparison.Ordinal));

        cut.Find($"button[aria-label=\"Actions for {connection.DisplayName}\"]").Click();
        _popoverProvider.WaitForElements(".mud-menu-item")
            .Single(item => item.TextContent.Contains("Preview sign-in", StringComparison.Ordinal))
            .Click();

        _dialogProvider.WaitForAssertion(() =>
        {
            Assert.Equal(connection.Id, _operations.PreviewedConnectionId);
            Assert.Equal("\"7\"", _operations.PreviewedIfMatch);
            Assert.Contains("Open preview sign-in", _dialogProvider.Markup, StringComparison.Ordinal);
            Assert.Contains(
                "https://elsa.example.test/external-authentication/previews/preview-handle/authorize",
                _dialogProvider.Markup,
                StringComparison.Ordinal);
        });

        _dialogProvider.FindAll("button")
            .Single(button => button.TextContent.Contains("Get one-time preview result", StringComparison.Ordinal))
            .Click();
        _dialogProvider.WaitForAssertion(() =>
        {
            Assert.Equal("preview-handle", _operations.PreviewHandle);
            Assert.Contains("https://issuer.example.test", _dialogProvider.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ConnectionList_HidesUnavailableActionsWithoutLeavingEmptyDividers()
    {
        _permissions.AllowedPermissions = new HashSet<string>([ExternalAuthenticationPermissions.Read], StringComparer.Ordinal);
        var connection = CreateConnection();
        connection.Validity = "valid";
        _api.Adapters =
        [
            new AdapterDescriptor
            {
                Type = connection.AdapterType,
                Capabilities = new AdapterCapabilities { SupportsTest = true, SupportsPreview = true }
            }
        ];
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [connection] });

        var cut = Render<ConnectionIndex>();
        cut.WaitForAssertion(() => Assert.Contains(connection.DisplayName, cut.Markup, StringComparison.Ordinal));

        cut.Find($"button[aria-label=\"Actions for {connection.DisplayName}\"]").Click();
        _popoverProvider.WaitForAssertion(() =>
        {
            Assert.Contains("Manage", _popoverProvider.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Test connection", _popoverProvider.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Preview sign-in", _popoverProvider.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Enable", _popoverProvider.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Archive", _popoverProvider.Markup, StringComparison.Ordinal);
            Assert.Empty(_popoverProvider.FindAll(".mud-divider"));
        });
    }

    [Theory]
    [InlineData("database", false, true, "Database", "Overrides deployment")]
    [InlineData("configuration", true, false, "Deployment", "Shadowed by Database")]
    [InlineData("database", true, false, "Database", "Shadowed by deployment")]
    [InlineData("database", false, false, "Database", null)]
    public void ConnectionListPresentation_DescribesOwnershipRelationships(
        string source,
        bool shadowed,
        bool overridesDeployment,
        string expectedOwnership,
        string? expectedRelationship)
    {
        var connection = CreateConnection(source);
        connection.Shadowed = shadowed;
        connection.OverridesConfigurationConnection = overridesDeployment;

        Assert.Equal(expectedOwnership, ConnectionListPresentation.OwnershipLabel(connection));
        Assert.Equal(expectedRelationship, ConnectionListPresentation.OwnershipRelationship(connection));
    }

    [Fact]
    public void ConnectionListPresentation_RejectsIncompleteAndSelfReferentialRelationshipMetadata()
    {
        var connection = CreateConnection();
        connection.ShadowedBy = new ConnectionReference { Id = connection.Id, DisplayName = "Self" };
        connection.Shadows =
        [
            new ConnectionReference { Id = "missing-name" },
            new ConnectionReference { Id = "deployment", DisplayName = "Deployment connection" }
        ];

        Assert.Null(ConnectionListPresentation.GetShadowingConnection(connection));
        Assert.Equal("deployment", Assert.Single(ConnectionListPresentation.GetShadowedConnections(connection)).Id);
    }

    [Theory]
    [InlineData(false, false, true, true, "valid", "Available", Color.Success)]
    [InlineData(false, false, true, false, "invalid", "Needs attention", Color.Error)]
    [InlineData(false, false, false, false, "valid", "Disabled", Color.Default)]
    [InlineData(true, false, true, true, "valid", "Archived", Color.Default)]
    [InlineData(false, true, true, false, "valid", "Shadowed", Color.Warning)]
    public void ConnectionListPresentation_SummarizesAvailability(
        bool archived,
        bool shadowed,
        bool enabledIntent,
        bool effectivelyEnabled,
        string validity,
        string expectedLabel,
        Color expectedColor)
    {
        var connection = CreateConnection();
        connection.Archived = archived;
        connection.Shadowed = shadowed;
        connection.EnabledIntent = enabledIntent;
        connection.EffectivelyEnabled = effectivelyEnabled;
        connection.Validity = validity;

        Assert.Equal(expectedLabel, ConnectionListPresentation.AvailabilityLabel(connection));
        Assert.Equal(expectedColor, ConnectionListPresentation.AvailabilityColor(connection));
        Assert.False(string.IsNullOrWhiteSpace(ConnectionListPresentation.AvailabilityIcon(connection)));
        Assert.Contains(
            $"{ConnectionStatusPresentation.LifecycleLabel(connection)} · {ConnectionStatusPresentation.ValidityLabel(connection.Validity)}",
            ConnectionListPresentation.AvailabilityDetailLabel(connection),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConnectionList_DoesNotApplyAnOlderFilterResponseAfterANewerOneCompletes()
    {
        var initial = new TaskCompletionSource<ListConnectionsResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var filtered = new TaskCompletionSource<ListConnectionsResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        _api.PendingListResults.Enqueue(initial.Task);
        _api.PendingListResults.Enqueue(filtered.Task);

        var cut = Render<ConnectionIndex>();
        cut.WaitForAssertion(() => Assert.Single(_api.ListRequests));

        var source = cut.FindComponent<MudSelect<string>>().Instance;
        var filterTask = cut.InvokeAsync(async () => await source.ValueChanged.InvokeAsync("database"));
        cut.WaitForAssertion(() => Assert.Equal(2, _api.ListRequests.Count));

        filtered.SetResult(new ListConnectionsResponse
        {
            Items = [new ConnectionSummary { Id = "new", Key = "new", DisplayName = "Newest result", AdapterType = "custom" }]
        });
        await filterTask;
        cut.WaitForAssertion(() => Assert.Contains("Newest result", cut.Markup, StringComparison.Ordinal));

        initial.SetResult(new ListConnectionsResponse
        {
            Items = [new ConnectionSummary { Id = "old", Key = "old", DisplayName = "Stale result", AdapterType = "custom" }]
        });
        cut.WaitForAssertion(() => Assert.DoesNotContain("Stale result", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public async Task ConnectionList_ReloadsAndResetsPagingWhenFiltersChange()
    {
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [CreateConnection()], NextCursor = "cursor-2" });
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [CreateConnection()], NextCursor = "cursor-3" });
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [CreateConnection()], NextCursor = null });
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [CreateConnection()], NextCursor = null });
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [CreateConnection()], NextCursor = null });

        var cut = Render<ConnectionIndex>();
        cut.WaitForAssertion(() => Assert.Single(_api.ListRequests));
        cut.Find("button[aria-label=\"Next page\"]").Click();
        cut.WaitForAssertion(() => Assert.Equal(2, _api.ListRequests.Count));
        Assert.Contains("Page 2", cut.Find(".mud-table-pagination").TextContent, StringComparison.Ordinal);

        var source = cut.FindComponent<MudSelect<string>>().Instance;
        await cut.InvokeAsync(() => source.ValueChanged.InvokeAsync("database"));
        cut.WaitForAssertion(() => Assert.Equal("database", _api.ListRequests[2].Source));
        Assert.Null(_api.ListRequests[2].Cursor);
        Assert.Contains("Page 1", cut.Find(".mud-table-pagination").TextContent, StringComparison.Ordinal);
        Assert.True(cut.Find("button[aria-label=\"First page\"]").HasAttribute("disabled"));

        var includeArchived = cut.FindComponent<MudCheckBox<bool>>().Instance;
        await cut.InvokeAsync(() => includeArchived.ValueChanged.InvokeAsync(true));
        cut.WaitForAssertion(() => Assert.Null(_api.ListRequests[3].Archived));
        Assert.Null(_api.ListRequests[3].Cursor);

        var search = cut.FindComponent<MudTextField<string>>().Instance;
        await cut.InvokeAsync(() => search.ValueChanged.InvokeAsync("contoso"));
        cut.WaitForAssertion(() => Assert.Equal("contoso", _api.ListRequests[4].Search));
        Assert.Null(_api.ListRequests[4].Cursor);
    }

    [Fact]
    public void ConnectionList_LabelsShadowedDatabaseStateAsStored()
    {
        var connection = CreateConnection();
        connection.Shadowed = true;
        connection.EnabledIntent = true;
        connection.Validity = "valid";
        var configuration = CreateConnection("configuration");
        configuration.CanCreateOverride = true;
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [connection, configuration] });

        var cut = Render<ConnectionIndex>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Database", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Shadowed by deployment", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Shadowed", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Stored: Enabled · Valid", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ConfigurationConnection_OffersManagementOfAnExistingStudioOverrideInsteadOfACopy()
    {
        var configuration = CreateConnection("configuration");
        configuration.CanCreateOverride = true;
        _api.GetResult = configuration;
        _api.Adapters = [CreateAdapter()];
        _api.ListResults.Enqueue(new ListConnectionsResponse
        {
            Items = [new ConnectionSummary
            {
                Id = "stored-override",
                Key = configuration.Key,
                Source = "database",
                AdapterType = configuration.AdapterType,
                DisplayName = "Stored override",
                Scope = new ConnectionScope { Kind = "host" },
                Shadowed = true
            }]
        });

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, configuration.Id));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Manage existing Database record", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Create full Database override", cut.Markup, StringComparison.Ordinal);
        });

        cut.FindAll("button").Single(button => button.TextContent.Contains("Manage existing Database record", StringComparison.Ordinal)).Click();
        Assert.EndsWith("/security/external-authentication/connections/stored-override", Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfigurationConnection_OffersRestorationOfAnArchivedStudioOverrideInsteadOfACopy()
    {
        var configuration = CreateConnection("configuration");
        configuration.CanCreateOverride = true;
        _api.GetResult = configuration;
        _api.Adapters = [CreateAdapter()];
        _api.ListResults.Enqueue(new ListConnectionsResponse
        {
            Items =
            [
                new ConnectionSummary
                {
                    Id = "archived-override",
                    Key = configuration.Key,
                    Source = "database",
                    AdapterType = configuration.AdapterType,
                    DisplayName = "Archived override",
                    Scope = new ConnectionScope { Kind = "host" },
                    Archived = true,
                    Shadowed = true
                }
            ]
        });

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, configuration.Id));

        cut.WaitForAssertion(() =>
        {
            Assert.Null(_api.ListRequests.Single().Archived);
            Assert.Contains("archived Database record already exists", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Review and restore Database record", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Create full Database override", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Theory]
    [InlineData("Disable and revoke sessions", true)]
    [InlineData("Disable only", false)]
    public void ConnectionList_RequiresAnExplicitSessionDecisionWhenConfirmingDisable(string action, bool revokeActiveSessions)
    {
        var connection = CreateConnection();
        connection.EnabledIntent = true;
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [connection] });

        var cut = Render<ConnectionIndex>();
        cut.WaitForAssertion(() => Assert.DoesNotContain("Revoke active sessions when disabling", cut.Markup, StringComparison.Ordinal));

        cut.Find($"button[aria-label=\"Actions for {connection.DisplayName}\"]").Click();
        var disable = _popoverProvider.WaitForElements(".mud-menu-item")
            .Single(item => item.TextContent.Contains("Disable", StringComparison.Ordinal));
        disable.Click();
        _dialogProvider.WaitForAssertion(() => Assert.Contains(action, _dialogProvider.Markup, StringComparison.Ordinal));
        _dialogProvider.FindAll("button").Single(button => button.TextContent.Contains(action, StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => Assert.Equal(revokeActiveSessions, _operations.RevokeActiveSessions));
    }

    [Fact]
    public void ConnectionEditor_OffersSessionRevocationWhenConfirmingDisable()
    {
        var connection = CreateConnection();
        connection.EnabledIntent = true;
        _api.GetResult = connection;
        _api.Adapters = [CreateAdapter()];

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, connection.Id));
        cut.WaitForAssertion(() => Assert.Contains("Disable", cut.Markup, StringComparison.Ordinal));

        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Disable").Click();
        _dialogProvider.WaitForAssertion(() => Assert.Contains("Disable and revoke sessions", _dialogProvider.Markup, StringComparison.Ordinal));
        _dialogProvider.FindAll("button").Single(button => button.TextContent.Contains("Disable and revoke sessions", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => Assert.True(_operations.RevokeActiveSessions));
    }

    [Fact]
    public async Task SecurityMenu_PlacesIdentityProviderConnectionsFirstWhenReadIsAllowed()
    {
        var menu = new ExternalAuthenticationSecurityMenuContributor(
            new FeatureProvider(true),
            new PermissionService(ExternalAuthenticationPermissions.Read));

        var item = Assert.Single(await menu.GetMenuItemsAsync());
        Assert.Equal("security/external-authentication/connections", item.Href);
        Assert.Equal("Identity provider connections", item.Text);
        Assert.Equal(100, item.Order);

        var hidden = await new ExternalAuthenticationSecurityMenuContributor(new FeatureProvider(true), new PermissionService(false)).GetMenuItemsAsync();
        Assert.Empty(hidden);
    }

    [Fact]
    public async Task SecurityMenu_PreservesPermissionGatesAndOrdersIdentityAndAccessItems()
    {
        var menu = new ExternalAuthenticationSecurityMenuContributor(
            new FeatureProvider(true),
            new PermissionService(
                ExternalAuthenticationPermissions.Read,
                ExternalAuthenticationPermissions.ManageLinks,
                ExternalAuthenticationPermissions.SessionsRead));

        var items = (await menu.GetMenuItemsAsync()).ToList();
        Assert.Equal(
            ["Identity provider connections", "External identity links", "Authentication sessions"],
            items.Select(x => x.Text));
        Assert.Equal([100f, 200f, 300f], items.Select(x => x.Order));

        var sessionsOnly = await new ExternalAuthenticationSecurityMenuContributor(
                new FeatureProvider(true),
                new PermissionService(ExternalAuthenticationPermissions.SessionsRead))
            .GetMenuItemsAsync();
        Assert.Equal("Authentication sessions", Assert.Single(sessionsOnly).Text);
    }

    [Fact]
    public void ConnectionPages_ExposeOnlyCanonicalRoutes()
    {
        Assert.Equal(
            ["/security/external-authentication/connections"],
            RoutesFor<ConnectionIndex>());
        Assert.Equal(
            [
                "/security/external-authentication/connections/new",
                "/security/external-authentication/connections/{ConnectionId}"
            ],
            RoutesFor<ConnectionEdit>());
    }

    private static string[] RoutesFor<T>() =>
        typeof(T)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Select(x => x.Template)
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static async Task<Refit.ApiException> CreateApiExceptionAsync(HttpStatusCode statusCode, string content)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://elsa.example.test/external-authentication/connections");
        using var response = new HttpResponseMessage(statusCode)
        {
            RequestMessage = request,
            Content = new StringContent(content)
        };
        return await Refit.ApiException.Create(request, HttpMethod.Post, response, new Refit.RefitSettings());
    }

    private (IRenderedComponent<DescriptorField> Component, Dictionary<string, JsonElement> Settings) RenderTagsArrayField()
    {
        var settings = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            ["scopes"] = JsonSerializer.SerializeToElement(Array.Empty<string>())
        };
        var component = Render<DescriptorField>(parameters => parameters
            .Add(field => field.Field, new ConnectionFieldDescriptor
            {
                Name = "scopes",
                DisplayName = "Scopes",
                ValueType = "string-array",
                UiHint = "tags"
            })
            .Add(field => field.Settings, settings));

        return (component, settings);
    }

    private static ConnectionDetail CreateConnection(string source = "database") => new()
    {
        Id = "connection-1",
        Source = source,
        DisplayName = "Contoso",
        Key = "contoso",
        AdapterType = "openid-connect",
        Scope = new ConnectionScope { Kind = "host" },
        Revision = 7
    };

    private static AdapterDescriptor CreateAdapter(bool includeSecret = false, bool includeUnsafe = false)
    {
        var descriptor = new AdapterDescriptor
        {
            Type = "openid-connect",
            DisplayName = "OpenID Connect",
            Fields = [new ConnectionFieldDescriptor { Name = "authority", DisplayName = "Authority", IsRequired = true }]
        };

        if (includeSecret)
            descriptor.Fields.Add(new ConnectionFieldDescriptor { Name = "clientSecret", DisplayName = "Client secret", IsSecretBinding = true, IsRequired = true });
        if (includeUnsafe)
            descriptor.Fields.Add(new ConnectionFieldDescriptor { Name = "allowInsecureMetadata", DisplayName = "Allow insecure metadata", IsUnsafe = true });
        return descriptor;
    }

    private static ConnectionMutation CreateMutation() => new()
    {
        Key = "contoso",
        AdapterType = "openid-connect",
        DisplayName = "Contoso"
    };

    private static void AssertOutlinedDense(Variant variant, Margin margin)
    {
        Assert.Equal(Variant.Outlined, variant);
        Assert.Equal(Margin.Dense, margin);
    }

    private static void RecheckConfiguration(IRenderedComponent<ConnectionEdit> cut) =>
        cut.Find(".connection-workspace__diagnostics")
            .QuerySelectorAll("button")
            .Single(button => button.TextContent.Contains("Re-check configuration", StringComparison.Ordinal))
            .Click();

    private static void ChangeField(IRenderedComponent<ConnectionEdit> cut, string label, string value) =>
        FindField(cut, label).Change(value);

    private static string? FieldValue(IRenderedComponent<ConnectionEdit> cut, string label) =>
        FindField(cut, label).GetAttribute("value");

    private static AngleSharp.Dom.IElement FindField(IRenderedComponent<ConnectionEdit> cut, string label)
    {
        var fieldLabel = cut.FindAll("label").Single(element => element.TextContent.Contains(label, StringComparison.Ordinal));
        return cut.Find($"#{fieldLabel.GetAttribute("for")}");
    }

    private sealed class FeatureProvider(bool enabled) : IRemoteFeatureProvider
    {
        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) => Task.FromResult(enabled && featureName == Feature.RemoteFeatureName);
        public Task<IEnumerable<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<FeatureDescriptor>>([]);
    }

    private sealed class PermissionService : IExternalAuthenticationPermissionService
    {
        public PermissionService(bool allowed) => Allowed = allowed;

        public PermissionService(params string[] permissions)
        {
            Allowed = true;
            AllowedPermissions = permissions.ToHashSet(StringComparer.Ordinal);
        }

        public bool Allowed { get; set; }
        public IReadOnlySet<string>? AllowedPermissions { get; set; }

        public ValueTask<bool> HasAsync(string permission, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Allowed && (AllowedPermissions is null || AllowedPermissions.Contains(permission)));
        public ValueTask<IReadOnlySet<string>> ListAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlySet<string>>(
                Allowed
                    ? AllowedPermissions ?? new HashSet<string>(["*"], StringComparer.Ordinal)
                    : new HashSet<string>(StringComparer.Ordinal));
    }

    private sealed class CustomEditorRegistration(string key, int contractVersion, Type componentType) : ICustomConnectionEditorRegistration
    {
        public string Key => key;
        public int ContractVersion => contractVersion;
        public Type ComponentType => componentType;
    }

    private sealed class TestCustomEditor : ComponentBase, IConnectionCustomEditor
    {
        [Parameter] public ConnectionDetail Connection { get; set; } = default!;
        [Parameter] public AdapterDescriptor Adapter { get; set; } = default!;
        [Parameter] public ConnectionMutation Model { get; set; } = default!;
        [Parameter] public bool ReadOnly { get; set; }
        [Parameter] public bool CanConfigureUnsafeSettings { get; set; }
        [Parameter] public bool CanCreateOverride { get; set; }
        [Parameter] public ICollection<ManagedSecretResolverDescriptor> ManagedSecretResolvers { get; set; } = [];
        [Parameter] public string? ManagedSecretResolverError { get; set; }
        [Parameter] public EventCallback<ConnectionMutation> Saved { get; set; }
        [Parameter] public EventCallback<(string Field, ManagedSecretMutation Secret)> ManagedSecretChanged { get; set; }
        [Parameter] public EventCallback<string> SecretBindingRemoved { get; set; }
        [Parameter] public EventCallback FullOverrideRequested { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "Legacy custom editor");
    }

    private sealed class ChangeTrackingCustomEditor : ComponentBase, IConnectionCustomEditorWithChangeTracking
    {
        [Parameter] public ConnectionDetail Connection { get; set; } = default!;
        [Parameter] public AdapterDescriptor Adapter { get; set; } = default!;
        [Parameter] public ConnectionMutation Model { get; set; } = default!;
        [Parameter] public bool ReadOnly { get; set; }
        [Parameter] public bool CanConfigureUnsafeSettings { get; set; }
        [Parameter] public bool CanCreateOverride { get; set; }
        [Parameter] public ICollection<ManagedSecretResolverDescriptor> ManagedSecretResolvers { get; set; } = [];
        [Parameter] public string? ManagedSecretResolverError { get; set; }
        [Parameter] public EventCallback<ConnectionMutation> Saved { get; set; }
        [Parameter] public EventCallback<(string Field, ManagedSecretMutation Secret)> ManagedSecretChanged { get; set; }
        [Parameter] public EventCallback<string> SecretBindingRemoved { get; set; }
        [Parameter] public EventCallback FullOverrideRequested { get; set; }
        [Parameter] public EventCallback Changed { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "Change-tracking custom editor");
    }

    private sealed class TestCustomEditorRegistry : ICustomConnectionEditorRegistry
    {
        public Type? ComponentType { get; set; }

        public bool TryResolve(CustomEditorContract? contract, out Type componentType)
        {
            componentType = ComponentType!;
            return contract is not null && ComponentType is not null;
        }
    }

    private sealed class TestBackendApiClientProvider(
        IExternalAuthenticationConnectionsApi connectionsApi,
        IExternalAuthenticationOperationsApi operationsApi) : IBackendApiClientProvider
    {
        public Uri Url { get; } = new("https://elsa.example.test/elsa/api/");

        public ValueTask<T> GetApiAsync<T>(CancellationToken cancellationToken = default) where T : class =>
            ValueTask.FromResult(typeof(T) == typeof(IExternalAuthenticationOperationsApi)
                ? (T)(object)operationsApi
                : (T)(object)connectionsApi);
    }

    private sealed class TestOperationsApi : IExternalAuthenticationOperationsApi
    {
        public bool? RevokeActiveSessions { get; private set; }
        public string? TestedConnectionId { get; private set; }
        public string? TestedIfMatch { get; private set; }
        public string? PreviewedConnectionId { get; private set; }
        public string? PreviewedIfMatch { get; private set; }
        public string? PreviewHandle { get; private set; }
        public Exception? TestException { get; set; }

        public Task DisableWithRecoveryOverrideAsync(
            string connectionId,
            string ifMatch,
            bool confirmFinalLoginPathOverride,
            bool revokeActiveSessions = false,
            CancellationToken cancellationToken = default)
        {
            RevokeActiveSessions = revokeActiveSessions;
            return Task.CompletedTask;
        }

        public Task<ConnectionTestResult> TestAsync(string connectionId, string ifMatch, CancellationToken cancellationToken = default)
        {
            TestedConnectionId = connectionId;
            TestedIfMatch = ifMatch;
            if (TestException is not null)
                throw TestException;
            return Task.FromResult(new ConnectionTestResult
            {
                Status = "succeeded",
                Summary = "Provider metadata was resolved.",
                TestedMaterialRevision = "material-7"
            });
        }
        public Task<PreviewInitiation> InitiatePreviewAsync(string connectionId, string ifMatch, CancellationToken cancellationToken = default)
        {
            PreviewedConnectionId = connectionId;
            PreviewedIfMatch = ifMatch;
            return Task.FromResult(new PreviewInitiation
            {
                NavigationUrl = "/external-authentication/previews/preview-handle/authorize",
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5)
            });
        }

        public Task<PreviewResultDocument> GetPreviewResultAsync(string previewHandle, CancellationToken cancellationToken = default)
        {
            PreviewHandle = previewHandle;
            return Task.FromResult(new PreviewResultDocument
            {
                Issuer = "https://issuer.example.test",
                MaskedSubject = "sub•••123",
                PolicyDecision = "would-link"
            });
        }
        public Task<ListExternalAuthenticationSessionsResponse> ListSessionsAsync(string? userId = null, string? connectionId = null, string? status = null, string? cursor = null, int pageSize = 25, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RevokeSessionAsync(string sessionId, RevokeExternalAuthenticationSessionRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestConnectionsApi : IExternalAuthenticationConnectionsApi, IIdentityRolesApi
    {
        public Queue<ListConnectionsResponse> ListResults { get; } = new();
        public Queue<Task<ListConnectionsResponse>> PendingListResults { get; } = new();
        public Queue<ConnectionDetail> GetResults { get; } = new();
        public List<string> GetRequests { get; } = [];
        public List<ListRequest> ListRequests { get; } = [];
        public List<string?> Cursors { get; } = [];
        public ConnectionDetail? GetResult { get; set; }
        public Exception? GetException { get; set; }
        public int GetExceptionAfterRequest { get; set; } = int.MaxValue;
        public ExternalAuthenticationRuntimeDescriptor? Runtime { get; set; } = new()
        {
            ManagementContractVersion = 1,
            ProductVersion = "test",
            InformationalVersion = "test"
        };
        public ICollection<AdapterDescriptor> Adapters { get; set; } = [];
        public ICollection<UnlinkedIdentityPolicyDescriptor> Policies { get; set; } = [];
        public ICollection<IdentityRoleOption> Roles { get; set; } = [];
        public ICollection<ManagedSecretResolverDescriptor> ManagedSecretResolvers { get; set; } =
            [new() { Type = "elsa-secrets", DisplayName = "Elsa Secrets" }];
        public int RuntimeRequests { get; private set; }
        public ConnectionValidationResult ValidationResult { get; set; } = new() { Valid = true };
        public Exception? CreateException { get; set; }
        public ConnectionDetail? CreateResult { get; set; }
        public int CreateRequests { get; private set; }
        public ConnectionMutation? CreatedRequest { get; private set; }
        public string? UpdatedConnectionId { get; private set; }
        public ConnectionMutation? UpdatedRequest { get; private set; }
        public string? UpdatedIfMatch { get; private set; }
        public string? ArchivedConnectionId { get; private set; }
        public string? ArchivedIfMatch { get; private set; }
        public string? ValidatedConnectionId { get; private set; }
        public Exception? EnableException { get; set; }
        public string? EnabledConnectionId { get; private set; }
        public string? EnabledIfMatch { get; private set; }
        public int EnableRequests { get; private set; }
        public int ValidationRequests { get; private set; }
        public TaskCompletionSource<ConnectionValidationResult>? PendingValidation { get; set; }

        public Task<ListConnectionsResponse> ListAsync(string? search = null, string? source = null, string? scope = null, string? adapterType = null, bool? enabled = null, bool? valid = null, bool? shadowed = null, bool? archived = null, string? cursor = null, int pageSize = 25, CancellationToken cancellationToken = default)
        {
            Cursors.Add(cursor);
            ListRequests.Add(new ListRequest(search, source, archived, cursor, pageSize));
            if (PendingListResults.TryDequeue(out var pending))
                return pending;

            return Task.FromResult(ListResults.Dequeue());
        }

        public sealed record ListRequest(string? Search, string? Source, bool? Archived, string? Cursor, int PageSize);

        public Task<ConnectionDetail> GetAsync(string connectionId, CancellationToken cancellationToken = default)
        {
            GetRequests.Add(connectionId);
            if (GetRequests.Count >= GetExceptionAfterRequest && GetException is not null)
                return Task.FromException<ConnectionDetail>(GetException);
            return Task.FromResult(GetResults.TryDequeue(out var result) ? result : GetResult ?? throw new NotSupportedException());
        }
        public Task<ExternalAuthenticationRuntimeDescriptor> GetRuntimeAsync(CancellationToken cancellationToken = default)
        {
            RuntimeRequests++;
            return Task.FromResult(Runtime ?? throw new NotSupportedException());
        }
        public Task<ICollection<AdapterDescriptor>> GetAdaptersAsync(CancellationToken cancellationToken = default) => Task.FromResult(Adapters);
        public Task<ICollection<PermissionGrantSourceDescriptor>> GetPermissionSourcesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ICollection<UnlinkedIdentityPolicyDescriptor>> GetPoliciesAsync(CancellationToken cancellationToken = default) => Task.FromResult(Policies);
        public Task<ICollection<ExternalUserMatcherDescriptor>> GetUserMatchersAsync(CancellationToken cancellationToken = default) => Task.FromResult<ICollection<ExternalUserMatcherDescriptor>>([]);
        public Task<ManagedSecretResolverCatalog> GetManagedSecretResolversAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new ManagedSecretResolverCatalog { Items = ManagedSecretResolvers });
        public Task<ICollection<PermissionDescriptor>> GetPermissionsAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ConnectionDetail> CreateAsync(ConnectionMutation request, CancellationToken cancellationToken = default)
        {
            CreateRequests++;
            CreatedRequest = request;
            if (CreateException is not null)
                return Task.FromException<ConnectionDetail>(CreateException);
            return Task.FromResult(CreateResult ?? new ConnectionDetail { Id = "override-1", Key = request.Key, DisplayName = request.DisplayName, AdapterType = request.AdapterType });
        }
        public Task<ConnectionDetail> UpdateAsync(string connectionId, ConnectionMutation request, string ifMatch, CancellationToken cancellationToken = default)
        {
            UpdatedConnectionId = connectionId;
            UpdatedRequest = request;
            UpdatedIfMatch = ifMatch;
            if (GetResult is null)
                throw new NotSupportedException();

            GetResult.OverridesConfigurationConnection = request.OverridesConfigurationConnection;
            GetResult.Shadowed = false;
            return Task.FromResult(GetResult);
        }
        public Task EnableAsync(string connectionId, string ifMatch, CancellationToken cancellationToken = default)
        {
            EnableRequests++;
            EnabledConnectionId = connectionId;
            EnabledIfMatch = ifMatch;
            return EnableException is null ? Task.CompletedTask : Task.FromException(EnableException);
        }
        public Task DisableAsync(string connectionId, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ArchiveAsync(string connectionId, string ifMatch, CancellationToken cancellationToken = default)
        {
            ArchivedConnectionId = connectionId;
            ArchivedIfMatch = ifMatch;
            return Task.CompletedTask;
        }
        public Task RestoreAsync(string connectionId, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ConnectionValidationResult> ValidateAsync(string connectionId, CancellationToken cancellationToken = default)
        {
            ValidationRequests++;
            ValidatedConnectionId = connectionId;
            return PendingValidation?.Task ?? Task.FromResult(ValidationResult);
        }
        public Task<ConnectionDetail> ReplaceManagedSecretAsync(string connectionId, string fieldName, ManagedSecretMutation request, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RemoveSecretBindingAsync(string connectionId, string fieldName, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        Task<IdentityRoleOptionsResponse> IIdentityRolesApi.ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new IdentityRoleOptionsResponse { Roles = Roles });
    }
}
