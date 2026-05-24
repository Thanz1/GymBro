using GymBro.Contracts.Events;

namespace GymBro.Identity.API.Email;

public interface IWelcomeEmailSender
{
    Task SendWelcomeEmailAsync(UserCreatedIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}
