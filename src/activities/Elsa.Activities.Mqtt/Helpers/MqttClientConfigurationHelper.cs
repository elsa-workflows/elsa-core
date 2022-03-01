using System;
using System.Text;

namespace Elsa.Activities.Mqtt.Helpers
{
    public static class MqttClientConfigurationHelper
    {
        public static string GetClientId(string activityId) => Convert.ToBase64String(Encoding.ASCII.GetBytes(activityId))[..22];
        public static string GetTestClientId(string activityId) => $"t{Convert.ToBase64String(Encoding.ASCII.GetBytes(activityId))[..22]}";
    }
}
