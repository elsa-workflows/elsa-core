using MQTTnet.Protocol;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;

namespace Elsa.Activities.Mqtt.Activities
{
    public abstract class MqttBaseActivity : Activity
    {
        [ActivityInput(
            Hint = "Topic",
            Order = 1,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string Topic { get; set; } = default!;


        [ActivityInput(
            Hint = "MQTT broker hostname",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid },
            Order = 1,
            Category = PropertyCategories.Configuration)]
        public string Host { get; set; } = default!;

        [ActivityInput(
            Hint = "MQTT broker port",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid },
            Order = 2,
            DefaultValue = 1883,
            Category = PropertyCategories.Configuration)]
        public int Port { get; set; } = default!;

        [ActivityInput(
            Hint = "Username",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid },
            Order = 3,
            Category = PropertyCategories.Configuration)]
        public string Username { get; set; } = default!;

        [ActivityInput(
            Hint = "Password",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid },
            Order = 4,
            Category = PropertyCategories.Configuration)]
        public string Password { get; set; } = default!;

        [ActivityInput(
            Hint = "Quality Of Service",
            UIHint = ActivityInputUIHints.Dropdown,
            Order = 5,
            Category = PropertyCategories.Configuration)]
        public MqttQualityOfServiceLevel QualityOfService { get; set; } = default;
    }
}
