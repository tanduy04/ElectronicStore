using ElectronicStore.Api.Data;
using ElectronicStore.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStore.Api.Repositories
{
    public class EmployeeRepository : Repository<Employee>, IEmployeeRepository
    {
        public EmployeeRepository(ElectronicStoreContext context) : base(context) { }

        public async Task<Employee?> GetByAccountIdAsync(int accountId)
        {
            return await _dbSet.FirstOrDefaultAsync(e => e.AccountId == accountId);
        }
    }
}
