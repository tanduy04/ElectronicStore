using ElectronicStore.Api.Data;

namespace ElectronicStore.Api.Repositories.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<Product?> GetProductWithDetailsAsync(int productId);
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);
        Task<IEnumerable<Product>> GetProductsByBrandAsync(int brandId);
        Task<IEnumerable<Product>> GetProductsBySupplierAsync(int supplierId);
        Task<IEnumerable<Product>> SearchProductsAsync(string keyword);
    }
}
