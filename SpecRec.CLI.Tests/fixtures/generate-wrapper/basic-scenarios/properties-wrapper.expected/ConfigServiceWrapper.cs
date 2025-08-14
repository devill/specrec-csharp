using System;

namespace TestProject
{
    public class ConfigServiceWrapper : IConfigService
    {
        private readonly ConfigService _wrapped;

        public ConfigServiceWrapper(ConfigService wrapped)
        {
            _wrapped = wrapped;
        }

        public string ConnectionString { get => _wrapped.ConnectionString; set => _wrapped.ConnectionString = value; }
        public int MaxRetries => _wrapped.MaxRetries;
        public bool IsEnabled => _wrapped.IsEnabled;

        public void UpdateConnectionString(string connectionString)
        {
            _wrapped.UpdateConnectionString(connectionString);
        }

        public void SetMaxRetries(int retries)
        {
            _wrapped.SetMaxRetries(retries);
        }

        public string GetSetting(string key, string defaultValue = "")
        {
            return _wrapped.GetSetting(key, defaultValue);
        }
    }
}