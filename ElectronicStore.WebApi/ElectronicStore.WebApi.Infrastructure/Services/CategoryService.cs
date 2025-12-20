using ElectronicStore.WebApi.Domain.Entities;
using ElectronicStore.WebApi.Infrastructure.Data.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectronicStore.WebApi.Infrastructure.Services
{
    public class CategoryService : ICategoryService
    {
        IRepository<Categories> _categoryRepository;
        public CategoryService(IRepository<Categories> categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }
        public async Task<IEnumerable<Categories>> GetCategories()
        {
            return await _categoryRepository.GetData();
        }
    }
}
