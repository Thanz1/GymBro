using System.Text;
using System.Text.Json;
using GymBro.Contracts.Events;
using GymBro.Identity.API.Email;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GymBro.Identity.API.Messaging;

public sealed class UserCreatedWelcomeEmailConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly RabbitMqOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserCreatedWelcomeEmailConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public UserCreatedWelcomeEmailConsumer(
        IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<UserCreatedWelcomeEmailConsumer> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);
        _channel.QueueDeclare(
            queue: _options.WelcomeEmailQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false);
        _channel.QueueBind(
            queue: _options.WelcomeEmailQueueName,
            exchange: _options.ExchangeName,
            routingKey: IntegrationEventRoutingKeys.UserCreated);
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += OnMessageReceivedAsync;

        _channel.BasicConsume(
            queue: _options.WelcomeEmailQueueName,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation(
            "Welcome email consumer listening queue '{Queue}'",
            _options.WelcomeEmailQueueName);

        return Task.CompletedTask;
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs args)
    {
        if (_channel is null)
        {
            return;
        }

        try
        {
            var json = Encoding.UTF8.GetString(args.Body.ToArray());
            var integrationEvent = JsonSerializer.Deserialize<UserCreatedIntegrationEvent>(json, JsonOptions);
            if (integrationEvent is null || string.IsNullOrWhiteSpace(integrationEvent.Email))
            {
                _logger.LogWarning("Invalid UserCreatedIntegrationEvent payload: {Payload}", json);
                _channel.BasicNack(args.DeliveryTag, multiple: false, requeue: false);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var emailSender = scope.ServiceProvider.GetRequiredService<IWelcomeEmailSender>();
            await emailSender.SendWelcomeEmailAsync(integrationEvent, CancellationToken.None);

            _channel.BasicAck(args.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process welcome email message.");
            _channel.BasicNack(args.DeliveryTag, multiple: false, requeue: true);
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
