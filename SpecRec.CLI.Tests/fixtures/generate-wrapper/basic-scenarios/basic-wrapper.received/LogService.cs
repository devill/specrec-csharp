using System;
using System.Collections.Generic;

namespace TestProject
{
    public class LogService
    {
        private readonly string _logPath;
        private readonly List<string> _entries;

        public LogService(string logPath)
        {
            _logPath = logPath ?? throw new ArgumentNullException(nameof(logPath));
            _entries = new List<string>();
        }

        public LogService(string logPath, int initialCapacity) : this(logPath)
        {
            _entries = new List<string>(initialCapacity);
        }

        public void LogInfo(string message)
        {
            _entries.Add($"[INFO] {DateTime.Now}: {message}");
        }

        public void LogError(string message, Exception exception = null)
        {
            var errorMessage = $"[ERROR] {DateTime.Now}: {message}";
            if (exception != null)
            {
                errorMessage += $" - {exception.Message}";
            }
            _entries.Add(errorMessage);
        }

        public string[] GetEntries()
        {
            return _entries.ToArray();
        }

        public void Clear()
        {
            _entries.Clear();
        }
    }
}