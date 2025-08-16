using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestProject
{
    public interface IUserServiceWrapper
    {
        void InitializeUserDatabase();
        string GetUserName(int userId);
        Task<bool> CreateUserAsync(int userId, string userName);
        void DeleteUser(int userId);
        bool UserExists(int userId);
        int GetUserCount();
        void ClearCache();
    }
}