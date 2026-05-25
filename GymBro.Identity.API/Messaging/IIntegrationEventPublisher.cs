using GymBro.Contracts.Events;

namespace GymBro.Identity.API.Messaging;

public interface IIntegrationEventPublisher
{
    Task PublishUserCreatedAsync(UserCreatedIntegrationEvent integrationEvent);
}
