using System;

namespace EdgeCases
{
    public class BaseService
    {
        protected string _connectionString = "";

        public virtual void Initialize(string connectionString)
        {
            _connectionString = connectionString;
        }

        public virtual bool IsConnected => !string.IsNullOrEmpty(_connectionString);

        public void Dispose()
        {
            _connectionString = "";
        }
    }
}