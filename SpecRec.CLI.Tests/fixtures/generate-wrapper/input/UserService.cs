using System;

namespace TestProject
{
    public class UserService
    {
        private readonly DatabaseService _databaseService;

        public UserService()
        {
            _databaseService = new DatabaseService();
        }

        public void InitializeUserDatabase()
        {
            _databaseService.Connect("user-db-connection");
        }
    }
}