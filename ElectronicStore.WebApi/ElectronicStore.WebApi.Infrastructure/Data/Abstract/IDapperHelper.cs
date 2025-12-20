using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectronicStore.WebApi.Infrastructure.Data.Abstract
{
    public interface IDapperHelper<T> where T : class
    {
        void ExecuteNotReturn(string query, DynamicParameters parameters = null);
        Task<T> ExecuteReturnScalar<T>(string query, DynamicParameters parameters = null);
        Task<IEnumerable<T>> ExecuteSqlReturnList<T>(string query, DynamicParameters parameters = null);
        Task<IEnumerable<T>> ExecuteStoreProdureReturnList<T>(string query, DynamicParameters parameters = null);
    }
}
