using ElectronicStore.Api.Dto;
using ElectronicStore.Api.Models;

namespace ElectronicStore.Api.Services.Interfaces
{
    public interface IProductService
    {
        Task<(bool Success, string Message, object? Data)> GetAllProductsAsync(
            int? categoryId, 
            int? brandId, 
            string? sortBy, 
            string? sortOrder, 
            int pageNumber, 
            int pageSize);
        
        Task<(bool Success, string Message, object? Data)> SearchProductsAsync(
            string search, 
            string? sortBy, 
            string? sortOrder, 
            int pageNumber, 
            int pageSize);
        
        Task<(bool Success, string Message, object? Data)> GetProductByIdAsync(int id);
        
        Task<(bool Success, string Message, object? Data)> CreateProductAsync(ProductDto dto);
        
        Task<(bool Success, string Message)> UpdateProductAsync(int id, ProductDto dto);
        
        Task<(bool Success, string Message)> DeleteProductAsync(int id);
        
        Task<(bool Success, string Message, object? Data)> GetProductsBySupplierAsync(
            int supplierId, 
            string? sortBy, 
            string? sortOrder, 
            int pageNumber, 
            int pageSize);
    }
}
