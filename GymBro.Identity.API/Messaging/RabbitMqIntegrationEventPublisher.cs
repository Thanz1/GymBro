using System.Text;
using System.Text.Json;
using GymBro.Contracts.Events;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace GymBro.Identity.API.Messaging;

public sealed class RabbitMqIntegrationEventPublisher : IIntegrationEventPublisher, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _exchangeName;
    private readonly string _userCreatedQueueName;
    private readonly ILogger<RabbitMqIntegrationEventPublisher> _logger;

    public RabbitMqIntegrationEventPublisher(
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqIntegrationEventPublisher> logger)
    {
        _logger = logger;
        var settings = options.Value;
        _exchangeName = settings.ExchangeName;
        _userCreatedQueueName = settings.UserCreatedQueueName;

        var factory = new ConnectionFactory
        {
            HostName = settings.HostName,
            Port = settings.Port,
            UserName = settings.UserName,
            Password = settings.Password,
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(
            exchange: _exchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);
        _channel.QueueDeclare(
            queue: _userCreatedQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false);
        _channel.QueueBind(
            queue: _userCreatedQueueName,
            exchange: _exchangeName,
            routingKey: IntegrationEventRoutingKeys.UserCreated);

        _logger.LogInformation(
            "RabbitMQ publisher connected to {Host}:{Port}, exchange '{Exchange}', queue '{Queue}'",
            settings.HostName,
            settings.Port,
            _exchangeName,
            _userCreatedQueueName);
    }

    public Task PublishUserCreatedAsync(UserCreatedIntegrationEvent integrationEvent)
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(integrationEvent, JsonOptions));
        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.MessageId = Guid.NewGuid().ToString();
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        properties.Type = nameof(UserCreatedIntegrationEvent);

        _channel.BasicPublish(
            exchange: _exchangeName,
            routingKey: IntegrationEventRoutingKeys.UserCreated,
            basicProperties: properties,
            body: body);

        _logger.LogInformation(
            "Published {EventName} for UserId={UserId}, Email={Email}, exchange={Exchange}, queue={Queue}, routingKey={RoutingKey}",
            nameof(UserCreatedIntegrationEvent),
            integrationEvent.UserId,
            integrationEvent.Email,
            _exchangeName,
            _userCreatedQueueName,
            IntegrationEventRoutingKeys.UserCreated);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }
}
