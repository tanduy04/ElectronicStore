using ElectronicStore.WebApi.Domain.Entities;
using ElectronicStore.WebApi.Infrastructure.Data.Abstract;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ElectronicStore.WebApi.Infrastructure.Data
{
    public class Repository<T> : IRepository<T> where T : class
    {
        ElectronicStoreContext _context;

        public Repository(ElectronicStoreContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<T>> GetData(Expression<Func<T, bool>> expression = null)
        {
            if(expression == null)
            {
                return await _context.Set<T>().ToListAsync();

            }
            return await _context.Set<T>().Where(expression).ToListAsync();
        }
        public async Task<T> GetById(object id)
        {
            return _context.Set<T>().Find(id);
        }
        public async Task<T> GetSingleConditionAsync(Expression<Func<T, bool>> expression=null)
        {
            return await _context.Set<T>().FirstOrDefaultAsync();
        }


        public async Task Insert(T entity)
        {
            EntityEntry<T> entityEntry = _context.Add(entity);
        }

        public async Task Insert(IEnumerable<T> entities)
        {
            await _context.Set<T>().AddRangeAsync(entities);  
        }

        public void Update(T entity)
        {
            _context.Update(entity);
        }
        public void Delete(T entity)
        {
            _context.Remove(entity);
        }

        public void Delete(Expression<Func<T, bool>> expression)
        {
            var entities = _context.Set<T>().Where(expression);
            _context.RemoveRange(entities);
        }
        public virtual IQueryable<T> Table => _context.Set<T>();    
        public async Task Commit()
        {
            await _context.SaveChangesAsync();
        }

    }
}
