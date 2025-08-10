using System;
using System.Collections.Generic;

namespace TestProject
{
    public interface ILogService
    {
        void LogInfo(string message);
        void LogError(string message, Exception exception = null);
        string[] GetEntries();
        void Clear();
    }
}