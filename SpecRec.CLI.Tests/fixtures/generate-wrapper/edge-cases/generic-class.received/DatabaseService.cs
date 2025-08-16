using System;
using System.Collections.Generic;

namespace EdgeCases
{
    public class DatabaseService : BaseService
    {
        protected Dictionary<string, object> _queryCache = new Dictionary<string, object>();

        public override void Initialize(string connectionString)
        {
            base.Initialize(connectionString);
            _queryCache.Clear();
        }

        public virtual void Connect()
        {
            Console.WriteLine($"Connecting with: {_connectionString}");
        }

        public virtual T ExecuteQuery<T>(string sql) where T : new()
        {
            if (_queryCache.ContainsKey(sql))
            {
                return (T)_queryCache[sql];
            }

            var result = new T();
            _queryCache[sql] = result;
            return result;
        }

        public void ClearCache()
        {
            _queryCache.Clear();
        }
    }
}