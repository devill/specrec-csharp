using System;

namespace TestProject
{
    public class ConfigService
    {
        public string ConnectionString { get; set; } = "";
        public int MaxRetries { get; private set; }
        public bool IsEnabled => !string.IsNullOrEmpty(ConnectionString);

        public void UpdateConnectionString(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public void SetMaxRetries(int retries)
        {
            MaxRetries = Math.Max(0, retries);
        }

        public string GetSetting(string key, string defaultValue = "")
        {
            return string.IsNullOrEmpty(key) ? defaultValue : $"setting-{key}";
        }
    }
}