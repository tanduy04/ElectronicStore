using ElectronicStore.WebApi.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ElectronicStore.WebApi.Infrastructure.Data.Abstract
{
    public interface IRepository<T> where T : class
    {
        void Delete(T entity);
        void Delete(Expression<Func<T, bool>> expression);
        Task<T> GetById(object id);
        void Update(T entity);
        Task<IEnumerable<T>> GetData(Expression<Func<T, bool>> expression = null);
        Task<T> GetSingleConditionAsync(Expression<Func<T, bool>> expression = null);
        Task Insert(T entity);
        Task Insert(IEnumerable<T> entities);
        Task Commit();
    }
}   
