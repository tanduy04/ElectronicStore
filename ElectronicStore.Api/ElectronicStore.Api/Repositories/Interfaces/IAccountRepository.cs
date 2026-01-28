using ElectronicStore.Api.Data;

namespace ElectronicStore.Api.Repositories.Interfaces
{
    public interface IAccountRepository : IRepository<Account>
    {
        Task<Account?> GetByEmailAsync(string email);
        Task<Account?> GetByUsernameAsync(string username);
        Task<Account?> GetByEmailOrUsernameAsync(string emailOrUsername);
        Task<Account?> GetAccountWithRoleAsync(int accountId);
    }
}
