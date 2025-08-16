using System;
using System.Collections.Generic;

namespace TestProject
{
    public class LogServiceWrapper : ILogServiceWrapper
    {
        private readonly LogService _wrapped;

        public LogServiceWrapper(LogService wrapped)
        {
            _wrapped = wrapped;
        }

        public void LogInfo(string message)
        {
            _wrapped.LogInfo(message);
        }

        public void LogError(string message, Exception exception = null)
        {
            _wrapped.LogError(message, exception);
        }

        public string[] GetEntries()
        {
            return _wrapped.GetEntries();
        }

        public void Clear()
        {
            _wrapped.Clear();
        }
    }
}