using Dapper;
using ElectronicStore.WebApi.Infrastructure.Data.Abstract;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectronicStore.WebApi.Infrastructure.Data
{
    public class DapperHelper<T> : IDapperHelper<T> where T : class
    {
        private readonly string connectionString = string.Empty;
        public DapperHelper(IConfiguration configuration)
        {
            connectionString= configuration.GetConnectionString("ElectronicStoreConnection");
        }
        public void ExecuteNotReturn(string query, DynamicParameters parameters = null)
        {
            using (var dbConnection = new SqlConnection(connectionString))
            {
                dbConnection.ExecuteAsync(query, parameters, commandType: CommandType.Text);

            }
        }

        public async Task<T> ExecuteReturnScalar<T>(string query, DynamicParameters parameters = null)
        {
            using (var dbConnection = new SqlConnection(connectionString))
            {
                return await dbConnection.ExecuteScalarAsync<T>(query, parameters, commandType: CommandType.Text);
            }
        }

        public Task<IEnumerable<T>> ExecuteSqlReturnList<T>(string query, DynamicParameters parameters = null)
        {
            using (var dbConnection = new SqlConnection(connectionString))
            {
                return dbConnection.QueryAsync<T>(query, parameters, commandType: CommandType.Text);
            }
        }

        public Task<IEnumerable<T>> ExecuteStoreProdureReturnList<T>(string query, DynamicParameters parameters = null)
        {
            using (var dbConnection = new SqlConnection(connectionString))
            {
                return dbConnection.QueryAsync<T>(query, parameters, commandType: CommandType.StoredProcedure);
            }
        }
    }
}
