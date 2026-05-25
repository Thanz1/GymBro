namespace GymBro.Identity.API.Messaging;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string ExchangeName { get; set; } = "gymbro.events";
    public string UserCreatedQueueName { get; set; } = "order.user-created";
    public string WelcomeEmailQueueName { get; set; } = "email.user-created";
}
