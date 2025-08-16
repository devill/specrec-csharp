using System;
using System.Data.SqlClient;

namespace EdgeCases
{
    public class SqlServerDatabaseService : DatabaseService
    {
        private SqlConnection _sqlConnection;

        public override void Initialize(string connectionString)
        {
            base.Initialize(connectionString);
            _sqlConnection = new SqlConnection(connectionString);
        }

        public override void Connect()
        {
            base.Connect();
            _sqlConnection?.Open();
        }

        public void ExecuteStoredProcedure(string procName, params object[] parameters)
        {
            Console.WriteLine($"Executing stored procedure: {procName} with {parameters.Length} parameters");
        }

        public int ExecuteNonQuery(string sql)
        {
            Console.WriteLine($"Executing non-query: {sql}");
            return 1; // Simulated affected rows
        }

        public void BeginTransaction()
        {
            Console.WriteLine("Beginning transaction");
        }

        public void CommitTransaction()
        {
            Console.WriteLine("Committing transaction");
        }

        public void RollbackTransaction()
        {
            Console.WriteLine("Rolling back transaction");
        }
    }
}