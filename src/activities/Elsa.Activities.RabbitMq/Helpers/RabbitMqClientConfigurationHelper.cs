namespace Elsa.Activities.RabbitMq.Helpers
{
    public static class RabbitMqClientConfigurationHelper
    {
        public static string GetClientId(string activityId) => $"Elsa-{activityId.ToUpper()}";
        public static string GetTestClientId(string activityId) => $"Elsa-Test-{activityId.ToUpper()}";
    }
}
