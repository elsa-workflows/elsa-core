namespace Elsa.MassTransit.Options;

public class RabbitMqOptions
{
    public const string RabbitMq = "RabbitMq";
    
    public string Username { get; set; }
    public string Password { get; set; }
}