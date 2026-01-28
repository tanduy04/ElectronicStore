using ElectronicStore.Api.Data;
using ElectronicStore.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStore.Api.Repositories
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        public ProductRepository(ElectronicStoreContext context) : base(context) { }

        public async Task<Product?> GetProductWithDetailsAsync(int productId)
        {
            return await _dbSet
                .Include(p => p.ProductImages)
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(p => p.ProductId == productId);
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            return await _dbSet
                .Where(p => p.CategoryId == categoryId)
                .Include(p => p.ProductImages)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsByBrandAsync(int brandId)
        {
            return await _dbSet
                .Where(p => p.BrandId == brandId)
                .Include(p => p.ProductImages)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsBySupplierAsync(int supplierId)
        {
            return await _dbSet
                .Where(p => p.SupplierId == supplierId)
                .Include(p => p.ProductImages)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> SearchProductsAsync(string keyword)
        {
            return await _dbSet
                .Where(p => p.ProductName.Contains(keyword))
                .Include(p => p.ProductImages)
                .ToListAsync();
        }
    }
}
