using Elsa.Mqtt.Utilities;

namespace Elsa.Mqtt.UnitTests.Utilities;

public class MqttTopicMatcherTests
{
    [Theory]
    [InlineData("sensors/temp", "sensors/temp", true)]
    [InlineData("sensors/temp", "sensors/humidity", false)]
    [InlineData("sensors/+/status", "sensors/device1/status", true)]
    [InlineData("sensors/+/status", "sensors/device1/data", false)]
    [InlineData("sensors/+/status", "sensors/a/b/status", false)]
    [InlineData("sensors/#", "sensors/temp", true)]
    [InlineData("sensors/#", "sensors/a/b/c", true)]
    [InlineData("#", "any/topic/at/all", true)]
    [InlineData("a/b/c", "a/b", false)]
    [InlineData("a/b", "a/b/c", false)]
    public void IsMatch_ReturnsExpectedResult(string filter, string topic, bool expected)
    {
        Assert.Equal(expected, MqttTopicMatcher.IsMatch(filter, topic));
    }

    [Fact]
    public void IsMatch_ExactTopic_ReturnsTrue()
    {
        Assert.True(MqttTopicMatcher.IsMatch("home/living-room/temperature", "home/living-room/temperature"));
    }

    [Fact]
    public void IsMatch_SingleLevelWildcard_MatchesExactlyOneLevel()
    {
        Assert.True(MqttTopicMatcher.IsMatch("home/+/temperature", "home/bedroom/temperature"));
        Assert.False(MqttTopicMatcher.IsMatch("home/+/temperature", "home/floor1/room/temperature"));
    }

    [Fact]
    public void IsMatch_MultiLevelWildcard_MatchesAllRemainingLevels()
    {
        Assert.True(MqttTopicMatcher.IsMatch("home/#", "home/living-room/lamp/status"));
        Assert.True(MqttTopicMatcher.IsMatch("home/#", "home"));
    }
}
