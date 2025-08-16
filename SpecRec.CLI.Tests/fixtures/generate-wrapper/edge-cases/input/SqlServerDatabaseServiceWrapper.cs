using System;
using System.Data.SqlClient;

namespace EdgeCases
{
    public class SqlServerDatabaseServiceWrapper : ISqlServerDatabaseService
    {
        private readonly SqlServerDatabaseService _wrapped;

        public SqlServerDatabaseServiceWrapper(SqlServerDatabaseService wrapped)
        {
            _wrapped = wrapped;
        }

        public void Initialize(string connectionString)
        {
            _wrapped.Initialize(connectionString);
        }

        public void Connect()
        {
            _wrapped.Connect();
        }

        public void ExecuteStoredProcedure(string procName, params object[] parameters)
        {
            _wrapped.ExecuteStoredProcedure(procName, parameters);
        }

        public int ExecuteNonQuery(string sql)
        {
            return _wrapped.ExecuteNonQuery(sql);
        }

        public void BeginTransaction()
        {
            _wrapped.BeginTransaction();
        }

        public void CommitTransaction()
        {
            _wrapped.CommitTransaction();
        }

        public void RollbackTransaction()
        {
            _wrapped.RollbackTransaction();
        }
    }
}