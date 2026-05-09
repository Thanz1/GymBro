namespace GymBro.Application.Layout
{
    public interface IAdminLayoutService
    {
        Task<AdminLayoutData> GetAsync();
    }
}
