using ElectronicStore.Api.Data;

namespace ElectronicStore.Api.Repositories.Interfaces
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<Order?> GetOrderWithDetailsAsync(int orderId);
        Task<IEnumerable<Order>> GetOrdersByCustomerAsync(int customerId);
    }
}
