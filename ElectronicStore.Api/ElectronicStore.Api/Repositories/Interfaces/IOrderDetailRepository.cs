using ElectronicStore.Api.Data;

namespace ElectronicStore.Api.Repositories.Interfaces
{
    public interface IOrderDetailRepository : IRepository<OrderDetail>
    {
        Task<IEnumerable<OrderDetail>> GetByOrderIdAsync(int orderId);
    }
}
