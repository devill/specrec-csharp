using System;

namespace TestProject
{
    public class DatabaseServiceWrapper : IDatabaseService
    {
        private readonly DatabaseService _wrapped;
        public DatabaseServiceWrapper(DatabaseService wrapped)
        {
            _wrapped = wrapped;
        }

        public void Connect(string connectionString)
        {
            _wrapped.Connect(connectionString);
        }
    }
}