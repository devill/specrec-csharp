using System;
using System.Data.SqlClient;

namespace EdgeCases
{
    public interface ISqlServerDatabaseService
    {
        void Initialize(string connectionString);
        void Connect();
        void ExecuteStoredProcedure(string procName, params object[] parameters);
        int ExecuteNonQuery(string sql);
        void BeginTransaction();
        void CommitTransaction();
        void RollbackTransaction();
    }
}