using ElectronicStore.Api.Data;
using ElectronicStore.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStore.Api.Repositories
{
    public class CustomerRepository : Repository<Customer>, ICustomerRepository
    {
        public CustomerRepository(ElectronicStoreContext context) : base(context) { }

        public async Task<Customer?> GetByPhoneAsync(string phone)
        {
            return await _dbSet.FirstOrDefaultAsync(c => c.Phone == phone);
        }

        public async Task<Customer?> GetByAccountIdAsync(int accountId)
        {
            return await _dbSet.FirstOrDefaultAsync(c => c.AccountId == accountId);
        }

        public async Task<Customer?> GetCustomerWithAccountAsync(int customerId)
        {
            return await _dbSet
                .Include(c => c.Account)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);
        }
    }
}
