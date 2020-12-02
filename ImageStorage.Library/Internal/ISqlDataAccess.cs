using System.Collections.Generic;
using System.Threading.Tasks;

namespace ImageStorage.Library.Internal
{
    public interface ISqlDataAccess
    {
        void CommitTransaction();
        string GetConnectionString(string name);
        void RollbackTransaction();
        Task<IEnumerable<int>> SaveDataInTransactionAsync<T>(string storedProcedure, T parameters);
        void StartTransaction();
    }
}