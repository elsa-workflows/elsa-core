using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.AzureServiceBus.Models
{
    public class MessageBody
    {
        public MessageBody()
        {

        }

        public MessageBody(string bodyType, byte[] data)
        {
            BodyType = bodyType;
            Data = data;
        }

        public string BodyType { get; set; }
        public byte[] Data { get; set; }
    }
}
