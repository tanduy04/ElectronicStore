using ElectronicStore.WebApi.Domain.Entities;
using ElectronicStore.WebApi.Infrastructure.Data.Abstract;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectronicStore.WebApi.Infrastructure.Data
{
    public class UnitOfWork : IUnitOfWork, IDisposable
    {
        ElectronicStoreContext _context;
        Repository<User> _repositoryUser;
        Repository<UserToken> _repositoryUserToken;
        private bool disposedValue;

        public UnitOfWork(ElectronicStoreContext electronicStoreContext)
        {
            _context = electronicStoreContext;
        }
        public Repository<User> RepositoryUser { get { return _repositoryUser ??= new Repository<User>(_context); } }
        public Repository<UserToken> RepositoryUserToken { get { return _repositoryUserToken ??= new Repository<UserToken>(_context); } }
        public async Task Commit()
        {
            await _context.SaveChangesAsync();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                   _context.Dispose();
                }
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
