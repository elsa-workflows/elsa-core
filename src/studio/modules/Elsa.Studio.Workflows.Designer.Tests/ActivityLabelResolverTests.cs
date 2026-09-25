using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Helpers;
using Xunit;

namespace Elsa.Studio.Workflows.Designer.Tests;

public sealed class ActivityLabelResolverTests
{
    [Fact]
    public void CustomDisplayText_TakesPrecedenceOverActivityName()
    {
        Assert.Equal("Send welcome e-mail", ActivityLabelResolver.Resolve("Send welcome e-mail", "SendEmail1", "Send Email"));
    }

    [Fact]
    public void ActivityName_IsUsedWhenNoDisplayTextIsSet()
    {
        Assert.Equal("SendEmail1", ActivityLabelResolver.Resolve(null, "SendEmail1", "Send Email"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankDisplayText_FallsBackToActivityName(string displayText)
    {
        Assert.Equal("SendEmail1", ActivityLabelResolver.Resolve(displayText, "SendEmail1", "Send Email"));
    }

    [Fact]
    public void TypeDisplayName_IsUsedWhenNeitherDisplayTextNorNameIsSet()
    {
        Assert.Equal("Send Email", ActivityLabelResolver.Resolve(null, null, "Send Email"));
    }

    [Fact]
    public void UnknownActivityLabel_IsUsedWhenNothingIsAvailable()
    {
        Assert.Equal(ActivityLabelResolver.UnknownActivityLabel, ActivityLabelResolver.Resolve(null, null, null));
    }

    [Fact]
    public void ResolvedLabels_AreTrimmed()
    {
        Assert.Equal("Send welcome e-mail", ActivityLabelResolver.Resolve("  Send welcome e-mail  ", null, null));
        Assert.Equal("SendEmail1", ActivityLabelResolver.Resolve(null, "  SendEmail1  ", null));
    }

    [Fact]
    public void ActivityDisplayText_TakesPrecedenceOverActivityNameAndDescriptor()
    {
        var activity = CreateActivity("SendEmail1", "Send welcome e-mail");
        var descriptor = new ActivityDescriptor
        {
            Name = "SendEmail",
            DisplayName = "Send Email"
        };

        Assert.Equal("Send welcome e-mail", ActivityLabelResolver.Resolve(activity, descriptor));
    }

    [Fact]
    public void ActivityWithoutDisplayText_FallsBackToNameThenDescriptor()
    {
        var descriptor = new ActivityDescriptor
        {
            Name = "SendEmail",
            DisplayName = "Send Email"
        };

        Assert.Equal("SendEmail1", ActivityLabelResolver.Resolve(CreateActivity("SendEmail1", null), descriptor));
        Assert.Equal("Send Email", ActivityLabelResolver.Resolve(CreateActivity(null, null), descriptor));
        Assert.Equal(ActivityLabelResolver.UnknownActivityLabel, ActivityLabelResolver.Resolve(CreateActivity(null, null), null));
    }

    private static JsonObject CreateActivity(string? name, string? displayText)
    {
        var activity = new JsonObject
        {
            ["type"] = "Elsa.SendEmail",
            ["version"] = 1
        };

        if (name != null)
            activity["name"] = name;

        if (displayText != null)
            activity["metadata"] = new JsonObject
            {
                ["displayText"] = displayText
            };

        return activity;
    }
}
