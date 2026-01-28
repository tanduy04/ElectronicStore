using ElectronicStore.Api.Data;

namespace ElectronicStore.Api.Repositories.Interfaces
{
    public interface IEmployeeRepository : IRepository<Employee>
    {
        Task<Employee?> GetByAccountIdAsync(int accountId);
    }
}
