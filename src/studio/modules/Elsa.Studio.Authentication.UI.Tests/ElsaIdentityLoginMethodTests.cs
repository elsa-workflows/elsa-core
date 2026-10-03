using AngleSharp.Dom;
using Bunit;
using Elsa.Studio.Authentication.Abstractions.Models;
using Elsa.Studio.Authentication.ElsaIdentity.Contracts;
using Elsa.Studio.Authentication.ElsaIdentity.Models;
using Elsa.Studio.Authentication.ElsaIdentity.UI.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Elsa.Studio.Authentication.UI.Tests;

public sealed class ElsaIdentityLoginMethodTests : BunitContext, IAsyncLifetime
{
    private const string SignInButton = "button.mud-button-filled";
    private const int UserNameField = 0;
    private const int PasswordField = 1;

    private readonly PendingCredentialsValidator _validator = new();
    private readonly List<string> _failures = [];
    private readonly IRenderedComponent<ElsaIdentityLoginMethod> _cut;

    public ElsaIdentityLoginMethodTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ICredentialsValidator>(_validator);
        Services.AddSingleton<IJwtAccessor, InMemoryJwtAccessor>();
        Services.AddSingleton<AuthenticationStateProvider, AnonymousAuthenticationStateProvider>();

        var method = new LoginMethodDescriptor("elsa-identity", "elsa-identity", "elsa-identity", "Elsa account", "elsa", 0, true, string.Empty);
        _cut = Render<ElsaIdentityLoginMethod>(parameters => parameters.Add(
            x => x.Context,
            new LoginMethodComponentContext(method, "/workflows", message =>
            {
                _failures.Add(message);
                return Task.CompletedTask;
            })));
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void SignIn_ShowsBusyStateWhileCredentialsAreValidatedAndRecoversAfterFailure()
    {
        SubmitCredentials();

        _cut.WaitForAssertion(() =>
        {
            var button = _cut.Find(SignInButton);
            Assert.True(button.HasAttribute("disabled"));
            Assert.Equal("true", button.GetAttribute("aria-busy"));
            Assert.Contains("Signing in…", button.TextContent);
            Assert.NotNull(button.QuerySelector(".mud-progress-circular"));
            Assert.All(_cut.FindAll("input"), input => Assert.True(input.HasAttribute("disabled")));
        });
        Assert.Equal(1, _validator.Calls);

        _validator.Complete(new(false, null, null));

        _cut.WaitForAssertion(() =>
        {
            Assert.Equal(["Invalid credentials. Try again."], _failures);
            AssertIdle();
        });
    }

    [Fact]
    public void SignIn_NavigatesToTheReturnPathAndStaysBusyOnceCredentialsAreAccepted()
    {
        SubmitCredentials();
        _cut.WaitForAssertion(() => Assert.True(_cut.Find(SignInButton).HasAttribute("disabled")));

        _validator.Complete(new(true, "access-token", "refresh-token"));

        _cut.WaitForAssertion(() =>
            Assert.EndsWith("/workflows", Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal));
        Assert.Empty(_failures);
        Assert.True(_cut.Find(SignInButton).HasAttribute("disabled"));
    }

    [Fact]
    public void SignIn_WithMissingCredentials_NeverStartsTheRequestOrGoesBusy()
    {
        _cut.Find(SignInButton).Click();

        _cut.WaitForAssertion(AssertIdle);
        Assert.Equal(0, _validator.Calls);
    }

    [Theory]
    [InlineData(UserNameField)]
    [InlineData(PasswordField)]
    public void PressingEnterInEitherField_StartsSignIn(int field)
    {
        FillCredentials();

        _cut.FindAll("input")[field].KeyDown(Key.Enter);

        _cut.WaitForAssertion(() => Assert.True(_cut.Find(SignInButton).HasAttribute("disabled")));
        Assert.Equal(1, _validator.Calls);
    }

    [Fact]
    public void PressingEnterToConfirmAnImeComposition_DoesNotStartSignIn()
    {
        FillCredentials();

        _cut.FindAll("input")[PasswordField].KeyDown(new KeyboardEventArgs { Key = "Enter", IsComposing = true });

        AssertIdle();
        Assert.Equal(0, _validator.Calls);
    }

    [Fact]
    public void PressingEnterWithMissingCredentials_NeverStartsTheRequestOrGoesBusy()
    {
        _cut.FindAll("input")[PasswordField].KeyDown(Key.Enter);

        _cut.WaitForAssertion(AssertIdle);
        Assert.Equal(0, _validator.Calls);
    }

    [Fact]
    public void PressingEnterWhileSigningIn_DoesNotStartASecondRequest()
    {
        SubmitCredentials();
        _cut.WaitForAssertion(() => Assert.True(_cut.Find(SignInButton).HasAttribute("disabled")));

        _cut.FindAll("input")[PasswordField].KeyDown(Key.Enter);

        // A second request would reach the validator after the form validates, so let the first one finish before counting.
        _validator.Complete(new(false, null, null));
        _cut.WaitForAssertion(() =>
        {
            Assert.Equal(["Invalid credentials. Try again."], _failures);
            AssertIdle();
        });
        Assert.Equal(1, _validator.Calls);
    }

    private void SubmitCredentials()
    {
        FillCredentials();
        _cut.Find(SignInButton).Click();
    }

    private void FillCredentials()
    {
        _cut.FindAll("input")[UserNameField].Input("alice");
        _cut.FindAll("input")[PasswordField].Input("secret");
    }

    private void AssertIdle()
    {
        var button = _cut.Find(SignInButton);
        Assert.False(button.HasAttribute("disabled"));
        Assert.Equal("false", button.GetAttribute("aria-busy"));
        Assert.Equal("Sign in", button.TextContent.Trim());
        Assert.All(_cut.FindAll("input"), input => Assert.False(input.HasAttribute("disabled")));
    }

    private sealed class PendingCredentialsValidator : ICredentialsValidator
    {
        private readonly TaskCompletionSource<ValidateCredentialsResult> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Calls { get; private set; }

        public ValueTask<ValidateCredentialsResult> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            Calls++;
            return new(_result.Task);
        }

        public void Complete(ValidateCredentialsResult result) => _result.SetResult(result);
    }

    private sealed class InMemoryJwtAccessor : IJwtAccessor
    {
        private readonly Dictionary<string, string> _tokens = new();

        public ValueTask<string?> ReadTokenAsync(string name) => new(_tokens.GetValueOrDefault(name));

        public ValueTask WriteTokenAsync(string name, string token)
        {
            _tokens[name] = token;
            return ValueTask.CompletedTask;
        }

        public ValueTask ClearTokenAsync(string name)
        {
            _tokens.Remove(name);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class AnonymousAuthenticationStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(new()));
    }
}
