using ElectronicStore.Api.Data;

namespace ElectronicStore.Api.Repositories.Interfaces
{
    public interface ICustomerRepository : IRepository<Customer>
    {
        Task<Customer?> GetByPhoneAsync(string phone);
        Task<Customer?> GetByAccountIdAsync(int accountId);
        Task<Customer?> GetCustomerWithAccountAsync(int customerId);
    }
}
