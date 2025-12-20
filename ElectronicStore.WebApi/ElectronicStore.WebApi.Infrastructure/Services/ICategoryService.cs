using ElectronicStore.WebApi.Domain.Entities;

namespace ElectronicStore.WebApi.Infrastructure.Services
{
    public interface ICategoryService
    {
        Task<IEnumerable<Categories>> GetCategories();
    }
}