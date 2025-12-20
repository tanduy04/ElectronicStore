using ElectronicStore.WebApi.Domain.Entities;

namespace ElectronicStore.WebApi.Infrastructure.Services
{
    public interface IUserTokenService
    {
        Task<UserToken> CheckRefreshToken(string code);
        Task SaveToken(UserToken userToken);
        void UpdateUserToken(UserToken userToken);
        Task<UserToken> UserExist(int id);
    }
}