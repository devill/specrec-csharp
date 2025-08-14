using System;

namespace TestProject
{
    public interface IConfigService
    {
        string ConnectionString { get; set; }

        int MaxRetries { get; }

        bool IsEnabled { get; }

        void UpdateConnectionString(string connectionString);
        void SetMaxRetries(int retries);
        string GetSetting(string key, string defaultValue = "");
    }
}