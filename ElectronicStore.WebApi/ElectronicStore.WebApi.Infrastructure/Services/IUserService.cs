using ElectronicStore.WebApi.Domain.Entities;

namespace ElectronicStore.WebApi.Infrastructure.Services
{
    public interface IUserService
    {
        Task<User> CheckLogin(string username, string password);
        Task<User> FindById(int id);
        Task<User> FindByUserName(string username);
    }
}