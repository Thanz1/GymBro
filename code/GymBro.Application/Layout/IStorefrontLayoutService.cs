namespace GymBro.Application.Layout
{
    public interface IStorefrontLayoutService
    {
        Task<StorefrontLayoutData> GetAsync(int? userId);
    }
}
