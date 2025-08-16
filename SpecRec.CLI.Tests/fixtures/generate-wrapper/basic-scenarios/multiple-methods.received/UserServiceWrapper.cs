using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestProject
{
    public class UserServiceWrapper : IUserServiceWrapper
    {
        private readonly UserService _wrapped;

        public UserServiceWrapper(UserService wrapped)
        {
            _wrapped = wrapped;
        }

        public void InitializeUserDatabase()
        {
            _wrapped.InitializeUserDatabase();
        }

        public string GetUserName(int userId)
        {
            return _wrapped.GetUserName(userId);
        }

        public Task<bool> CreateUserAsync(int userId, string userName)
        {
            return _wrapped.CreateUserAsync(userId, userName);
        }

        public void DeleteUser(int userId)
        {
            _wrapped.DeleteUser(userId);
        }

        public bool UserExists(int userId)
        {
            return _wrapped.UserExists(userId);
        }

        public int GetUserCount()
        {
            return _wrapped.GetUserCount();
        }

        public void ClearCache()
        {
            _wrapped.ClearCache();
        }
    }
}