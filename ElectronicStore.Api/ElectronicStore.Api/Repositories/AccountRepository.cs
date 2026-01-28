using ElectronicStore.Api.Data;
using ElectronicStore.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStore.Api.Repositories
{
    public class AccountRepository : Repository<Account>, IAccountRepository
    {
        public AccountRepository(ElectronicStoreContext context) : base(context) { }

        public async Task<Account?> GetByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(a => a.Email == email);
        }

        public async Task<Account?> GetByUsernameAsync(string username)
        {
            return await _dbSet.FirstOrDefaultAsync(a => a.Username == username);
        }

        public async Task<Account?> GetByEmailOrUsernameAsync(string emailOrUsername)
        {
            return await _dbSet
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.Email == emailOrUsername || a.Username == emailOrUsername);
        }

        public async Task<Account?> GetAccountWithRoleAsync(int accountId)
        {
            return await _dbSet
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.AccountId == accountId);
        }
    }
}
