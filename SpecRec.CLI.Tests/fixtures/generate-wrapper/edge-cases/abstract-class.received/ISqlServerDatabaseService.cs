using System;
using System.Data.SqlClient;

namespace EdgeCases
{
    public interface ISqlServerDatabaseService
    {
        // Inherited from BaseService
        bool IsConnected { get; }

        void Initialize(string connectionString);
        void Dispose();
        // Inherited from DatabaseService
        void Connect();
        T ExecuteQuery<T>(string sql)
            where T : new();
        void ClearCache();
        // SqlServerDatabaseService specific methods
        void ExecuteStoredProcedure(string procName, params object[] parameters);
        int ExecuteNonQuery(string sql);
        void BeginTransaction();
        void CommitTransaction();
        void RollbackTransaction();
    }
}