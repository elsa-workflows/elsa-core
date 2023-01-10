namespace Elsa.Activities.Kafka.Helpers
{
    public static class KafkaClientConfigurationHelper
    {
        public static string GetClientId(string activityId) => $"Elsa-{activityId.ToUpper()}";
        public static string GetTestClientId(string activityId) => $"Elsa-Test-{activityId.ToUpper()}";
    }
}
