using GymBro.Core;

namespace GymBro.Application.Auth
{
    public interface ITokenService
    {
        string CreateToken(User user);
    }
}
