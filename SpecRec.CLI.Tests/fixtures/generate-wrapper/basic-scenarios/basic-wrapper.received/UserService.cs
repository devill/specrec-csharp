using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestProject
{
    public class UserService
    {
        private readonly DatabaseService _databaseService;
        private readonly Dictionary<int, string> _userCache;

        public UserService()
        {
            _databaseService = new DatabaseService();
            _userCache = new Dictionary<int, string>();
        }

        public void InitializeUserDatabase()
        {
            _databaseService.Connect("user-db-connection");
        }

        public string GetUserName(int userId)
        {
            if (_userCache.ContainsKey(userId))
            {
                return _userCache[userId];
            }
            
            var userName = $"User{userId}";
            _userCache[userId] = userName;
            return userName;
        }

        public async Task<bool> CreateUserAsync(int userId, string userName)
        {
            await Task.Delay(100); // Simulate async operation
            _userCache[userId] = userName;
            return true;
        }

        public void DeleteUser(int userId)
        {
            _userCache.Remove(userId);
        }

        public bool UserExists(int userId)
        {
            return _userCache.ContainsKey(userId);
        }

        public int GetUserCount()
        {
            return _userCache.Count;
        }

        public void ClearCache()
        {
            _userCache.Clear();
        }
    }
}