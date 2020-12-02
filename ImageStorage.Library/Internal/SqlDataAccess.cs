using Dapper;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace ImageStorage.Library.Internal
{
    public sealed class SqlDataAccess : ISqlDataAccess, IDisposable
    {
        private readonly IConfiguration _config;
        private const string ConnectionStringName = "ImageStorageDatabase";
        private IDbConnection _connection;
        private IDbTransaction _transaction;
        private bool isClosed = false;

        public SqlDataAccess(IConfiguration config)
        {
            _config = config;
        }

        public string GetConnectionString(string name)
        {
            var cnn = _config.GetConnectionString(name);

            return cnn;
        }

        public void StartTransaction()
        {
            string connectionString = GetConnectionString(ConnectionStringName);

            _connection = new SqlConnection(connectionString);
            _connection.Open();

            _transaction = _connection.BeginTransaction();

            isClosed = false;
        }

        public async Task<IEnumerable<int>> SaveDataInTransactionAsync<T>(string storedProcedure, T parameters)
        {
            var rows = await _connection.QueryAsync<int>(storedProcedure,
                                           parameters,
                                           commandType: CommandType.StoredProcedure,
                                           transaction: _transaction);
            return rows;
        }

        public void CommitTransaction()
        {
            _transaction?.Commit();
            _connection?.Close();

            isClosed = true;
        }

        public void RollbackTransaction()
        {
            _transaction.Rollback();
            _connection?.Close();

            isClosed = true;
        }
        public void Dispose()
        {
            if (isClosed)
            {
                try
                {
                    CommitTransaction();
                }
                catch
                {
                    _connection?.Close();
                }
            }
            _transaction = null;
            _connection = null;
        }
    }
}