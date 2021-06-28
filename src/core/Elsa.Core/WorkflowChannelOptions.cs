using System.Collections.Generic;

namespace Elsa
{
    public class WorkflowChannelOptions
    {
        public const string DefaultChannel = "Default";

        public WorkflowChannelOptions()
        {
            Channels = new List<string> { DefaultChannel };
        }
        
        public ICollection<string> Channels { get; set; }
        public string Default { get; set; } = DefaultChannel;

        public string GetChannelOrDefault(string? channel)
        {
            if (string.IsNullOrWhiteSpace(channel))
                return Default;

            if (!Channels.Contains(channel))
                return Default;

            return channel;
        }
    }
}